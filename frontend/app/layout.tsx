import type { Metadata } from "next";
import "./globals.css";

const geistSans = { variable: "--font-geist-sans" };
const geistMono = { variable: "--font-geist-mono" };

export const metadata: Metadata = {
  title: "OnboardFlow — Great partnerships start here.",
  description: "One place for your clients, documents and every step in between. Client onboarding & document workflow.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full bg-[#0d272d] text-white flex flex-col">{children}</body>
    </html>
  );
}
