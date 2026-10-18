// Synthetic examples. Run tsc with --noEmit --strict --target ES2022 --lib ES2022,DOM.
import type { ChefzAccountsResponse, ChefzImportResponse, ChefzRiderReport, ChefzOrderSettlementImportResponse, ChefzOperationProblem } from '../chefz-accounts-and-reports-contracts';

export const accounts = {
  "items": [
    {
      "accountId": "00000000-0000-4000-8000-000000000010",
      "externalAccountId": "CHEFZ-A",
      "reportIdNumber": "SAMPLE-1",
      "accountType": "FullTime",
      "commissionRate": 0,
      "dailyFee": 25,
      "hasPassword": false,
      "rowVersion": "AAAAAAAAAAE=",
      "sponsorId": "00000000-0000-4000-8000-000000000002",
      "operatingCityId": "00000000-0000-4000-8000-000000000003",
      "vehicleType": "Car",
      "dashboardStatus": "active",
      "status": "Assigned",
      "statusReason": null,
      "accountRowVersion": "AAAAAAAAAAI=",
      "isOperationallyBlocked": false,
      "operationalBlockReason": null
    },
    {
      "accountId": "00000000-0000-4000-8000-000000000011",
      "externalAccountId": "CHEFZ-B",
      "reportIdNumber": "SAMPLE-2",
      "accountType": "FullTime",
      "commissionRate": 0,
      "dailyFee": 25,
      "hasPassword": false,
      "rowVersion": "AAAAAAAAAAE=",
      "sponsorId": "00000000-0000-4000-8000-000000000002",
      "operatingCityId": "00000000-0000-4000-8000-000000000003",
      "vehicleType": "Car",
      "dashboardStatus": "active",
      "status": "Assigned",
      "statusReason": null,
      "accountRowVersion": "AAAAAAAAAAI=",
      "isOperationallyBlocked": false,
      "operationalBlockReason": null
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 2,
  "summary": {
    "total": 2,
    "taken": 2,
    "notTaken": 0,
    "available": 0,
    "assigned": 2,
    "suspended": 0,
    "retired": 0,
    "archived": 0
  },
  "sponsors": [
    {
      "sponsorId": "00000000-0000-4000-8000-000000000002",
      "sponsorNameAr": "كفيل تجريبي",
      "sponsorNameEn": "Example sponsor",
      "counts": {
        "total": 2,
        "taken": 2,
        "notTaken": 0,
        "available": 0,
        "assigned": 2,
        "suspended": 0,
        "retired": 0,
        "archived": 0
      }
    }
  ]
} satisfies ChefzAccountsResponse;

export const performanceImport = {
  "importId": "00000000-0000-4000-8000-000000000040",
  "reportDate": "2026-09-20",
  "importedRows": 2,
  "skippedRows": 0,
  "issues": [],
  "riders": [
    {
      "riderProfileId": "00000000-0000-4000-8000-000000000001",
      "riderName": "مندوب تجريبي",
      "accounts": [
        {
          "id": "00000000-0000-4000-8000-000000000030",
          "accountId": "00000000-0000-4000-8000-000000000010",
          "externalAccountId": "CHEFZ-A",
          "assignmentId": "00000000-0000-4000-8000-000000000020",
          "riderProfileId": "00000000-0000-4000-8000-000000000001",
          "riderName": "مندوب تجريبي",
          "reportDate": "2026-09-20",
          "reportIdNumber": "SAMPLE-1",
          "totalValidOffers": 4,
          "acceptedOffers": 2,
          "declinedOffers": 0,
          "missedOffers": 2,
          "acceptanceRate": 50
        },
        {
          "id": "00000000-0000-4000-8000-000000000031",
          "accountId": "00000000-0000-4000-8000-000000000011",
          "externalAccountId": "CHEFZ-B",
          "assignmentId": "00000000-0000-4000-8000-000000000021",
          "riderProfileId": "00000000-0000-4000-8000-000000000001",
          "riderName": "مندوب تجريبي",
          "reportDate": "2026-09-20",
          "reportIdNumber": "SAMPLE-2",
          "totalValidOffers": 6,
          "acceptedOffers": 3,
          "declinedOffers": 0,
          "missedOffers": 3,
          "acceptanceRate": 50
        }
      ]
    }
  ]
} satisfies ChefzImportResponse;

export const riderPerformanceReport = {
  "accountId": null,
  "riderId": "00000000-0000-4000-8000-000000000001",
  "requestedHalves": 1,
  "halves": [
    {
      "from": "2026-09-16",
      "to": "2026-09-30",
      "daysWithData": 1,
      "orders": 5,
      "acceptanceRate": 50,
      "rating": "D",
      "message": null
    }
  ],
  "selectedSummary": {
    "halvesWithData": 1,
    "daysWithData": 1,
    "orders": 5,
    "averageOrdersPerHalf": 5,
    "acceptanceRate": 50,
    "rating": "D"
  },
  "allTimeSummary": {
    "halvesWithData": 1,
    "daysWithData": 1,
    "orders": 5,
    "averageOrdersPerHalf": 5,
    "acceptanceRate": 50,
    "rating": "D"
  },
  "daily": [
    {
      "date": "2026-09-20",
      "orders": 5,
      "validOffers": 10,
      "missedOffers": 5,
      "acceptanceRate": 50,
      "rating": "D"
    }
  ],
  "messages": [],
  "accounts": [
    {
      "accountId": "00000000-0000-4000-8000-000000000010",
      "externalAccountId": "CHEFZ-A",
      "riderProfileId": "00000000-0000-4000-8000-000000000001",
      "riderName": "مندوب تجريبي",
      "summary": {
        "halvesWithData": 1,
        "daysWithData": 1,
        "orders": 2,
        "averageOrdersPerHalf": 2,
        "acceptanceRate": 50,
        "rating": "D"
      },
      "daily": [
        {
          "id": "00000000-0000-4000-8000-000000000030",
          "accountId": "00000000-0000-4000-8000-000000000010",
          "externalAccountId": "CHEFZ-A",
          "assignmentId": "00000000-0000-4000-8000-000000000020",
          "riderProfileId": "00000000-0000-4000-8000-000000000001",
          "riderName": "مندوب تجريبي",
          "reportDate": "2026-09-20",
          "reportIdNumber": "SAMPLE-1",
          "totalValidOffers": 4,
          "acceptedOffers": 2,
          "declinedOffers": 0,
          "missedOffers": 2,
          "acceptanceRate": 50
        }
      ]
    },
    {
      "accountId": "00000000-0000-4000-8000-000000000011",
      "externalAccountId": "CHEFZ-B",
      "riderProfileId": "00000000-0000-4000-8000-000000000001",
      "riderName": "مندوب تجريبي",
      "summary": {
        "halvesWithData": 1,
        "daysWithData": 1,
        "orders": 3,
        "averageOrdersPerHalf": 3,
        "acceptanceRate": 50,
        "rating": "D"
      },
      "daily": [
        {
          "id": "00000000-0000-4000-8000-000000000031",
          "accountId": "00000000-0000-4000-8000-000000000011",
          "externalAccountId": "CHEFZ-B",
          "assignmentId": "00000000-0000-4000-8000-000000000021",
          "riderProfileId": "00000000-0000-4000-8000-000000000001",
          "riderName": "مندوب تجريبي",
          "reportDate": "2026-09-20",
          "reportIdNumber": "SAMPLE-2",
          "totalValidOffers": 6,
          "acceptedOffers": 3,
          "declinedOffers": 0,
          "missedOffers": 3,
          "acceptanceRate": 50
        }
      ]
    }
  ]
} satisfies ChefzRiderReport;

export const moneyImport = {
  "importId": "00000000-0000-4000-8000-000000000060",
  "isPreview": false,
  "alreadyImported": false,
  "canSave": false,
  "sourceRows": 2,
  "riders": [
    {
      "riderProfileId": "00000000-0000-4000-8000-000000000001",
      "riderName": "مندوب تجريبي",
      "periodFrom": "2026-09-16",
      "periodTo": "2026-09-30",
      "sourceRows": 2,
      "deliveryFees": 300,
      "vatAmount": 45,
      "deliveryFeesAfterVat": 255,
      "tips": 30,
      "compensation": 6,
      "bonuses": 9,
      "deductions": 12,
      "netEarnings": 288,
      "companyAmount": 650,
      "riderAmount": -362,
      "accounts": [
        {
          "id": "00000000-0000-4000-8000-000000000050",
          "accountId": "00000000-0000-4000-8000-000000000010",
          "driverId": "SAMPLE-1",
          "assignmentId": "00000000-0000-4000-8000-000000000020",
          "riderProfileId": "00000000-0000-4000-8000-000000000001",
          "riderName": "مندوب تجريبي",
          "accountType": "FullTime",
          "periodFrom": "2026-09-16",
          "periodTo": "2026-09-30",
          "chargedFrom": "2026-09-16",
          "chargedTo": "2026-09-30",
          "assignedDays": 15,
          "sourceRows": 1,
          "deliveryFees": 100,
          "vatAmount": 15,
          "deliveryFeesAfterVat": 85,
          "tips": 10,
          "compensation": 2,
          "bonuses": 3,
          "deductions": 4,
          "netEarnings": 96,
          "companyAmount": 375,
          "riderAmount": -279
        },
        {
          "id": "00000000-0000-4000-8000-000000000051",
          "accountId": "00000000-0000-4000-8000-000000000011",
          "driverId": "SAMPLE-2",
          "assignmentId": "00000000-0000-4000-8000-000000000021",
          "riderProfileId": "00000000-0000-4000-8000-000000000001",
          "riderName": "مندوب تجريبي",
          "accountType": "FullTime",
          "periodFrom": "2026-09-16",
          "periodTo": "2026-09-30",
          "chargedFrom": "2026-09-20",
          "chargedTo": "2026-09-30",
          "assignedDays": 11,
          "sourceRows": 1,
          "deliveryFees": 200,
          "vatAmount": 30,
          "deliveryFeesAfterVat": 170,
          "tips": 20,
          "compensation": 4,
          "bonuses": 6,
          "deductions": 8,
          "netEarnings": 192,
          "companyAmount": 275,
          "riderAmount": -83
        }
      ]
    }
  ],
  "netEarnings": 288,
  "companyAmount": 650,
  "riderAmount": -362,
  "issues": []
} satisfies ChefzOrderSettlementImportResponse;

export const assignmentLimit = {
  "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "status": 409,
  "detail": "The rider already has the maximum number of active Chefz accounts.",
  "errorCode": "platform.rider_account_limit_reached",
  "correlationId": "synthetic-example",
  "field": "actualRiderProfileId",
  "assignmentLimit": {
    "riderProfileId": "00000000-0000-4000-8000-000000000001",
    "requestedAccountId": "00000000-0000-4000-8000-000000000012",
    "requestedPaymentModel": "PayPerOrder",
    "maximumActiveAccounts": 2,
    "maximumSalaryAccounts": 1,
    "activeAccounts": [
      {
        "assignmentId": "00000000-0000-4000-8000-000000000020",
        "accountId": "00000000-0000-4000-8000-000000000010",
        "platformId": "00000000-0000-4000-8000-000000000004",
        "platformCode": "SHIFTZ",
        "platformNameAr": "شفز",
        "platformNameEn": "Chefz",
        "externalAccountId": "CHEFZ-A",
        "paymentModel": "PayPerOrder"
      },
      {
        "assignmentId": "00000000-0000-4000-8000-000000000021",
        "accountId": "00000000-0000-4000-8000-000000000011",
        "platformId": "00000000-0000-4000-8000-000000000004",
        "platformCode": "SHIFTZ",
        "platformNameAr": "شفز",
        "platformNameEn": "Chefz",
        "externalAccountId": "CHEFZ-B",
        "paymentModel": "PayPerOrder"
      }
    ],
    "allowedPaymentModels": []
  }
} satisfies ChefzOperationProblem;
