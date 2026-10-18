import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const out = path.resolve('docs/chefz-account-updates-examples');
fs.mkdirSync(out, {recursive:true});
const guid = n => `00000000-0000-4000-8000-${String(n).padStart(12,'0')}`;
const rider = guid(1), sponsor = guid(2), ids=[guid(10),guid(11)], assignments=[guid(20),guid(21)];
const name='مندوب تجريبي';
const counts={total:2,taken:2,notTaken:0,available:0,assigned:2,suspended:0,retired:0,archived:0};
const accountRows=ids.map((accountId,i)=>({accountId,externalAccountId:`CHEFZ-${i?'B':'A'}`,reportIdNumber:`SAMPLE-${i+1}`,accountType:'FullTime',commissionRate:0,dailyFee:25,hasPassword:false,rowVersion:'AAAAAAAAAAE=',sponsorId:sponsor,operatingCityId:guid(3),vehicleType:'Car',dashboardStatus:'active',status:'Assigned',statusReason:null,accountRowVersion:'AAAAAAAAAAI=',isOperationallyBlocked:false,operationalBlockReason:null}));
const accounts={items:accountRows,page:1,pageSize:50,totalCount:2,summary:counts,sponsors:[{sponsorId:sponsor,sponsorNameAr:'كفيل تجريبي',sponsorNameEn:'Example sponsor',counts}]};
const daily=ids.map((accountId,i)=>({id:guid(30+i),accountId,externalAccountId:accountRows[i].externalAccountId,assignmentId:assignments[i],riderProfileId:rider,riderName:name,reportDate:'2026-09-20',reportIdNumber:accountRows[i].reportIdNumber,totalValidOffers:i?6:4,acceptedOffers:i?3:2,declinedOffers:0,missedOffers:i?3:2,acceptanceRate:50}));
const performance={importId:guid(40),reportDate:'2026-09-20',importedRows:2,skippedRows:0,issues:[],riders:[{riderProfileId:rider,riderName:name,accounts:daily}]};
const summary = orders => ({halvesWithData:1,daysWithData:1,orders,averageOrdersPerHalf:orders,acceptanceRate:50,rating:'D'});
const report={accountId:null,riderId:rider,requestedHalves:1,halves:[{from:'2026-09-16',to:'2026-09-30',daysWithData:1,orders:5,acceptanceRate:50,rating:'D',message:null}],selectedSummary:summary(5),allTimeSummary:summary(5),daily:[{date:'2026-09-20',orders:5,validOffers:10,missedOffers:5,acceptanceRate:50,rating:'D'}],messages:[],accounts:daily.map(d=>({accountId:d.accountId,externalAccountId:d.externalAccountId,riderProfileId:rider,riderName:name,summary:summary(d.acceptedOffers),daily:[d]}))};
const moneyAccounts=ids.map((accountId,i)=>({id:guid(50+i),accountId,driverId:accountRows[i].reportIdNumber,assignmentId:assignments[i],riderProfileId:rider,riderName:name,accountType:'FullTime',periodFrom:'2026-09-16',periodTo:'2026-09-30',chargedFrom:i?'2026-09-20':'2026-09-16',chargedTo:'2026-09-30',assignedDays:i?11:15,sourceRows:1,deliveryFees:i?200:100,vatAmount:i?30:15,deliveryFeesAfterVat:i?170:85,tips:i?20:10,compensation:i?4:2,bonuses:i?6:3,deductions:i?8:4,netEarnings:i?192:96,companyAmount:i?275:375,riderAmount:i?-83:-279}));
const amountKeys=['sourceRows','deliveryFees','vatAmount','deliveryFeesAfterVat','tips','compensation','bonuses','deductions','netEarnings','companyAmount','riderAmount'];
const totals=Object.fromEntries(amountKeys.map(k=>[k,moneyAccounts.reduce((a,x)=>a+x[k],0)]));
const money={importId:guid(60),isPreview:false,alreadyImported:false,canSave:false,sourceRows:2,riders:[{riderProfileId:rider,riderName:name,periodFrom:'2026-09-16',periodTo:'2026-09-30',...totals,accounts:moneyAccounts}],netEarnings:totals.netEarnings,companyAmount:totals.companyAmount,riderAmount:totals.riderAmount,issues:[]};
const limit={type:'https://httpstatuses.com/409',title:'Conflict',status:409,detail:'The rider already has the maximum number of active Chefz accounts.',errorCode:'platform.rider_account_limit_reached',correlationId:'synthetic-example',field:'actualRiderProfileId',assignmentLimit:{riderProfileId:rider,requestedAccountId:guid(12),requestedPaymentModel:'PayPerOrder',maximumActiveAccounts:2,maximumSalaryAccounts:1,activeAccounts:ids.map((accountId,i)=>({assignmentId:assignments[i],accountId,platformId:guid(4),platformCode:'SHIFTZ',platformNameAr:'شفز',platformNameEn:'Chefz',externalAccountId:accountRows[i].externalAccountId,paymentModel:'PayPerOrder'})),allowedPaymentModels:[]}};
const examples=[['accounts','ChefzAccountsResponse',accounts],['performance-import','ChefzImportResponse',performance],['rider-performance-report','ChefzRiderReport',report],['money-import','ChefzOrderSettlementImportResponse',money],['assignment-limit','ChefzOperationProblem',limit]];
let check=`// Synthetic examples. Run tsc with --noEmit --strict --target ES2022 --lib ES2022,DOM.\nimport type { ${examples.map(x=>x[1]).join(', ')} } from '../chefz-accounts-and-reports-contracts';\n`;
for (const [file,type,value] of examples) {
  fs.writeFileSync(path.join(out,file+'.json'),JSON.stringify(value,null,2)+'\n');
  check+=`\nexport const ${file.replace(/-([a-z])/g,(_,c)=>c.toUpperCase())} = ${JSON.stringify(value,null,2)} satisfies ${type};\n`;
}
fs.writeFileSync(path.join(out,'examples.typecheck.ts'),check);
assert.equal(accounts.summary.total,accounts.items.length);
assert.equal(accounts.summary.total,counts.available+counts.assigned+counts.suspended+counts.retired+counts.archived);
assert.equal(report.selectedSummary.orders,daily.reduce((a,d)=>a+d.acceptedOffers,0));
for(const a of moneyAccounts) {
 assert.equal(a.deliveryFees-a.vatAmount,a.deliveryFeesAfterVat);
 assert.equal(a.deliveryFeesAfterVat+a.tips+a.compensation+a.bonuses-a.deductions,a.netEarnings);
 assert.equal(a.assignedDays*25,a.companyAmount);
 assert.equal(a.netEarnings-a.companyAmount,a.riderAmount);
 assert.equal(a.riderProfileId,rider);
}
assert.equal(money.companyAmount,650); assert.equal(money.netEarnings,288); assert.equal(money.riderAmount,-362);
console.log('Generated 5 JSON examples; grouping, counts, performance and financial arithmetic checks passed.');
