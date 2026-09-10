import fs from 'node:fs';
const b=fs.readFileSync('references/vista-workshop.json');
function doc(start){let i=start+4;const result={};while(b[i]!==0){const type=b[i++];const end=b.indexOf(0,i);const key=b.toString('utf8',i,end);i=end+1;let value;
if(type===1){value=b.readDoubleLE(i);i+=8;}else if(type===2){const n=b.readInt32LE(i);value=b.toString('utf8',i+4,i+3+n);i+=4+n;}
else if(type===3||type===4){const n=b.readInt32LE(i);value=doc(i);if(type===4)value=Object.values(value);i+=n;}
else if(type===8){value=b[i++]!==0;}else if(type===10){value=null;}else if(type===16){value=b.readInt32LE(i);i+=4;}else if(type===18){value=Number(b.readBigInt64LE(i));i+=8;}else throw new Error(`BSON type ${type} at ${i}`);result[key]=value;}return result;}
const data=doc(0);fs.writeFileSync('references/vista-decks.json',JSON.stringify(data,null,2));
const seen=new Set();function inspect(x){if(!x||typeof x!=='object')return;if(x.FaceURL && !seen.has(x.FaceURL)){seen.add(x.FaceURL);console.log(JSON.stringify(x));}for(const y of Object.values(x))inspect(y);}inspect(data);
