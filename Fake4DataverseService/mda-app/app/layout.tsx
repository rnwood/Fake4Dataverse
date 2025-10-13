'use client';

import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import "./globals.css";

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <head>
        <title>Fake4Dataverse Model-Driven App</title>
        <meta name="description" content="Model-Driven App interface for Fake4Dataverse" />
      </head>
      <body style={{ margin: 0, padding: 0, height: '100vh', overflow: 'hidden' }}>
        <FluentProvider theme={webLightTheme}>
          {children}
        </FluentProvider>
      </body>
    </html>
  );
}
