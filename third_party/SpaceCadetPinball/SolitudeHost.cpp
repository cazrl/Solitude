#include "pch.h"
#include "SolitudeHost.h"
#include "winmain.h"
#include "pb.h"
#include "render.h"
#include "TPinballTable.h"
#include "TBall.h"
#include "options.h"
#include "pinball.h"
#include "midi.h"
#include "TFlipper.h"

HWND solitude::parent = nullptr;
HDC solitude::paintDC = nullptr;
wchar_t solitude::settings[MAX_PATH * 4]{};
wchar_t solitude::evidence[MAX_PATH * 4]{};

extern "C" __declspec(dllexport) int __cdecl SolitudeRun(HWND host)
{
    solitude::parent = host;
    GetEnvironmentVariableW(L"SOLITUDE_PINBALL_SETTINGS", solitude::settings, _countof(solitude::settings));
    GetEnvironmentVariableW(L"SOLITUDE_PINBALL_EVIDENCE", solitude::evidence, _countof(solitude::evidence));
    HMODULE module{};
    GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        reinterpret_cast<LPCWSTR>(&SolitudeRun), &module);
    char args[] = "";
    int result = winmain::WinMain(module, nullptr, args, SW_SHOW);
    timeEndPeriod(1);
    return result;
}

LRESULT solitude::message(HWND window, UINT msg, WPARAM wp, LPARAM lp, bool& handled)
{
    handled = true;
    // The table belongs to the worker process; WinForms' menu message filter
    // cannot see its clicks. Notify the host without swallowing game input.
    if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN || msg == WM_XBUTTONDOWN) {
        PostMessageW(GetParent(parent), WM_APP + 21, 0, 0);
    }
    if ((msg == WM_PRINT || msg == WM_PRINTCLIENT) && pb::MainTable) {
        paintDC=reinterpret_cast<HDC>(wp);pb::paint();paintDC=nullptr;return 0;
    }
    if (msg == WM_TIMER && wp == 777) {
        if (!IsWindow(parent)) PostMessageW(window, WM_CLOSE, 0, 0);
        return 0;
    }
    if (msg == WM_APP + 11) { // Explicit, idempotent pause for host menus/deactivation.
        if (wp && !winmain::single_step) winmain::pause();
        if (!wp && winmain::single_step) winmain::end_pause(true);
        return 0;
    }
    if (msg == WM_APP + 12) { // Read-only state, consumed by the host and integration tests.
        if (!pb::MainTable) return -1;
        auto table = pb::MainTable;
        switch (wp) {
        case 0: return 1;
        case 1: return table->CurScore;
        case 2: return winmain::single_step;
        case 3: return table->BallCount;
        case 4: return table->CurrentPlayer;
        case 5: return options::Options.Sounds;
        case 6: return options::Options.Music;
        case 7: return table->PlayerCount;
        case 8: return static_cast<LRESULT>(pb::time_now * 1000);
        case 9: return table->FlipperL->BmpIndex;
        case 10: return table->FlipperR->BmpIndex;
        case 11: { RECT client{};GetClientRect(window,&client);return client.right; }
        case 12: return *evidence != 0; // Automated workers suppress effects and music.
        }
        return 0;
    }
    if (msg == WM_APP + 13 && *evidence && pb::MainTable) {
        // Test-only output is enabled by a launch environment variable, never a message path.
        auto bmp = render::vscreen;
        wchar_t path[MAX_PATH * 4];
        swprintf_s(path, L"%s\\table.bmp", evidence);
        FILE* file{};
        if (_wfopen_s(&file, path, L"wb") == 0) {
            BITMAPFILEHEADER header{};
            header.bfType = 0x4d42;
            header.bfOffBits = sizeof(header) + sizeof(BITMAPINFOHEADER) + 256 * sizeof(RGBQUAD);
            header.bfSize = header.bfOffBits + bmp.Stride * bmp.Height;
            BITMAPINFOHEADER info{};
            info.biSize = sizeof(info); info.biWidth = bmp.Width; info.biHeight = bmp.Height;
            info.biPlanes = 1; info.biBitCount = 8; info.biSizeImage = bmp.Stride * bmp.Height;
            PALETTEENTRY palette[256]{};
            GetPaletteEntries(gdrv::palette_handle, 0, 256, palette);
            RGBQUAD colors[256]{};
            for (int i=0;i<256;i++) colors[i] = {palette[i].peBlue,palette[i].peGreen,palette[i].peRed,0};
            fwrite(&header,sizeof(header),1,file); fwrite(&info,sizeof(info),1,file);
            fwrite(colors,sizeof(colors),1,file); fwrite(bmp.BmpBufPtr1,info.biSizeImage,1,file); fclose(file);
        }
        swprintf_s(path, L"%s\\state.json", evidence);
        if (_wfopen_s(&file,path,L"w") == 0) {
            auto table=pb::MainTable; auto ball=table->BallList->Get(0);
            fprintf(file,"{\"score\":%d,\"balls\":%d,\"paused\":%d,\"time\":%.4f,\"x\":%.4f,\"y\":%.4f,\"speed\":%.4f}",
                table->CurScore,table->BallCount,winmain::single_step,pb::time_now,ball->Position.X,ball->Position.Y,ball->Speed);
            fclose(file);
        }
        return 1;
    }
    if (msg == WM_KEYDOWN && (wp == VK_F1 || wp == VK_F4 || wp == VK_F6)) {
        PostMessageW(GetParent(parent), WM_APP + 20, wp, 0); return 0;
    }
    handled = false; return 0;
}
