import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Al Bawaba Logistics ERP",
  description: "Operations workspace for Al Bawaba Logistics.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="ar" dir="rtl" suppressHydrationWarning>
      <body>{children}</body>
    </html>
  );
}
