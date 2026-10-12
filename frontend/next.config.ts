import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  images: {
    dangerouslyAllowLocalIP: process.env.NODE_ENV === "development",
    remotePatterns: [
      { protocol: "http", hostname: "localhost", port: "19000", pathname: "/culinary-blog/**" },
      { protocol: "http", hostname: "127.0.0.1", port: "19000", pathname: "/culinary-blog/**" },
      { protocol: "http", hostname: "localhost", port: "9000", pathname: "/culinary-blog/**" },
      { protocol: "http", hostname: "127.0.0.1", port: "9000", pathname: "/culinary-blog/**" },
      { protocol: "http", hostname: "localhost", port: "5059", pathname: "/uploads/**" },
      { protocol: "http", hostname: "127.0.0.1", port: "5059", pathname: "/uploads/**" },
      { protocol: "https", hostname: "images.unsplash.com" },
      { protocol: "https", hostname: "picsum.photos" },
      { protocol: "https", hostname: "ui-avatars.com" },
      { protocol: "https", hostname: "avatar.iran.liara.run" },
      { protocol: "http", hostname: "localhost", port: "5058" },
      { protocol: "http", hostname: "127.0.0.1", port: "5058" },
    ],
  },
};

export default nextConfig;

