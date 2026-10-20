import './globals.css';
import { Header } from '../components/Header';

export const metadata = {
  title: 'onboardFlow - Client Onboarding & Document Workflow Portal',
  description: 'Enterprise portal for client onboarding, secure document collection, and sequential review workflows.',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <Header />
        <main>{children}</main>
      </body>
    </html>
  );
}
