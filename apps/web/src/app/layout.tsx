import "./globals.css";
import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { StoreInitializer } from '@/components/store/StoreInitializer';
import { Providers } from './providers';
import { ChatProvider } from '@/components/chat';
import { TermsGuard } from '@/components/auth/TermsGuard';

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "HydroEspinaca - Gestión Inteligente de Cultivos Hidropónicos",
  description: "Plataforma de monitoreo y control para sistemas hidropónicos con inteligencia artificial",
  icons: {
    icon: '/favicon.ico',
  },
  other: {
    'facebook-domain-verification': 'sd6v4nbngxpmnichcupogglv8c9mvt',
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es">
      <head>
        <link rel="icon" href="/favicon.ico" sizes="any" />
      </head>
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <Providers>
          <StoreInitializer />
          <TermsGuard />
          {children}
          {/* Chat flotante — visible en todas las páginas autenticadas */}
          <ChatProvider />
        </Providers>
      </body>
    </html>
  );
}
