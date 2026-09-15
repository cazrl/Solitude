#pragma once
#include <Windows.h>
namespace solitude {
extern HWND parent;
extern HDC paintDC;
extern wchar_t settings[MAX_PATH * 4];
extern wchar_t evidence[MAX_PATH * 4];
LRESULT message(HWND window, UINT msg, WPARAM wp, LPARAM lp, bool& handled);
}
