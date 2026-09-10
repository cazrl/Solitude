// Inspect archival PE resources as data. Never load or execute the input.
import fs from 'node:fs';
for (const name of ['sol','freecell','spider']) {
 const b=fs.readFileSync(`references/Mechanics/${name}-xp.resource.bin`);
 const u16=p=>b.readUInt16LE(p),u32=p=>b.readUInt32LE(p);
 const pe=u32(60),opt=pe+24,sections=opt+u16(pe+20);
 function offset(rva){for(let i=0;i<u16(pe+6);i++){const s=sections+i*40,start=u32(s+12);if(rva>=start&&rva<start+Math.max(u32(s+8),u32(s+16)))return u32(s+20)+rva-start;}throw Error('Invalid RVA');}
 const root=offset(u32(opt+(u16(opt)===0x20b?112:96)+16)),records=[],raw=[];
 function str(p){let v='';while(u16(p)){v+=String.fromCharCode(u16(p));p+=2;}return [v,p+2];}
 function label(id){return id&0x80000000?b.toString('utf16le',root+(id&0x7fffffff)+2,root+(id&0x7fffffff)+2+2*u16(root+(id&0x7fffffff))):id;}
 function walk(rel,keys=[]){let p=root+rel;for(let i=0;i<u16(p+12)+u16(p+14);i++){const e=p+16+i*8,id=u32(e),child=u32(e+4),ids=[...keys,label(id)];if(child&0x80000000)walk(child&0x7fffffff,ids);else{
   const d=root+child,start=offset(u32(d)),size=u32(d+4);
   raw.push({type:ids[0],id:ids[1],data:b.subarray(start,start+size)});
   if(![4,9].includes(ids[0]))records.push({type:ids[0],id:ids[1],bytes:size,header:b.toString('ascii',start,start+4)});
   if(b.toString('ascii',start,start+4)==='RIFF') {fs.mkdirSync('src/Assets/Sounds',{recursive:true});fs.writeFileSync(`src/Assets/Sounds/${name}-${ids[1]}.wav`,b.subarray(start,start+size));}
   if(ids[0]===9){const entries=[];for(let x=start;x<start+size;x+=8)entries.push({flags:u16(x),key:u16(x+2),command:u16(x+4)});records.push({type:'accelerators',id:ids[1],entries});}
   if(ids[0]===6){let p=start;const entries=[];for(let i=0;i<16;i++){const length=u16(p);p+=2;const text=b.toString('utf16le',p,p+2*length);p+=2*length;if(text)entries.push({id:(ids[1]-1)*16+i,text});}records.push({type:'strings',id:ids[1],entries});}
   if(ids[0]===4 && u16(start)===0){let pos=start+4+u16(start+2);function menu(){const items=[];let flags;do{flags=u16(pos);pos+=2;let command=null;if(!(flags&16)){command=u16(pos);pos+=2;}const [text,next]=str(pos);pos=next;items.push({text,command,flags,...(flags&16?{children:menu()}:{})});}while(!(flags&128));return items;}records.push({type:'menu',id:ids[1],items:menu()});}
   if(ids[0]===5){
    let p=start;const extended=u16(p+2)===65535;
    let style,count;if(extended){style=u32(p+12);count=u16(p+16);p+=18;}else{style=u32(p);count=u16(p+8);p+=10;}
    function rect(){const r={x:b.readInt16LE(p),y:b.readInt16LE(p+2),w:b.readInt16LE(p+4),h:b.readInt16LE(p+6)};p+=8;return r;}
    function value(){if(u16(p)===65535){const v={ordinal:u16(p+2)};p+=4;return v;}const [v,next]=str(p);p=next;return v;}
    const bounds=rect(),menu=value(),windowClass=value(),caption=value();let font=null;
    if(style&64){const points=u16(p);p+=2;let weight=400,italic=0,charset=1;if(extended){weight=u16(p);italic=b[p+2];charset=b[p+3];p+=4;}font={points,weight,italic,charset,face:value()};}
    const items=[];for(let i=0;i<count;i++){p=(p+3)&~3;let itemStyle,exStyle;if(extended){itemStyle=u32(p+8);exStyle=u32(p+4);p+=12;}else{itemStyle=u32(p);exStyle=u32(p+4);p+=8;}const bounds=rect(),id=extended?u32(p):u16(p);p+=extended?4:2;const windowClass=value(),text=value(),extra=u16(p);p+=extra||2;items.push({id,style:itemStyle,exStyle,bounds,windowClass,text});}
    records.push({type:'dialog',id:ids[1],style,bounds,caption,font,items});
   }
 }}}
 walk(0);
 const group=raw.find(r=>r.type===14);if(group)for(const classic of [false,true]){
   const entries=[];for(let i=0;i<group.data.readUInt16LE(4);i++){const e=group.data.subarray(6+i*14,20+i*14);if(classic&&e.readUInt16LE(6)>4)continue;entries.push({e,image:raw.find(r=>r.type===3&&r.id===e.readUInt16LE(12)).data});}
   if(!entries.length)continue;const header=Buffer.alloc(6+entries.length*16);header.writeUInt16LE(1,2);header.writeUInt16LE(entries.length,4);let position=header.length;
   entries.forEach(({e,image},i)=>{e.copy(header,6+i*16,0,12);header.writeUInt32LE(position,18+i*16);position+=image.length;});
   fs.mkdirSync('src/Assets/Icons',{recursive:true});fs.writeFileSync(`src/Assets/Icons/${name}${classic?'-classic':''}.ico`,Buffer.concat([header,...entries.map(e=>e.image)]));
 }
 fs.writeFileSync(`references/Mechanics/${name}-xp-controls.json`,JSON.stringify(records,null,2));console.log(name,JSON.stringify(records));
}
