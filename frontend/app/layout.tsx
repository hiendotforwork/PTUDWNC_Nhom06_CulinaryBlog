import type { Metadata } from "next";
import { Playfair_Display, Plus_Jakarta_Sans } from "next/font/google";
import "./globals.css";
import { AppProvider } from "./context/AppContext";
import { Shell } from "./components/layout/Shell";

const playfair = Playfair_Display({
  variable: "--font-serif",
  subsets: ["latin", "vietnamese"],
  display: "swap",
});

const plusJakarta = Plus_Jakarta_Sans({
  variable: "--font-sans",
  subsets: ["latin", "vietnamese"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "Culinary Blog - Blog Ẩm Thực & Nấu Ăn",
  description:
    "Khám phá hàng ngàn công thức nấu ăn ngon, mẹo vặt nhà bếp và chia sẻ đam mê ẩm thực truyền thống và hiện đại.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="vi"
      className={`${playfair.variable} ${plusJakarta.variable} antialiased`}
    >
      <body className="min-h-[100dvh]">
        <AppProvider>
          <Shell>{children}</Shell>
        </AppProvider>
      </body>
    </html>
  );
}
