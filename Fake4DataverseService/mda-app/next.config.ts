import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'export',
  distDir: '../src/Fake4Dataverse.Service/wwwroot/mda',
  basePath: '',  // No base path - will serve from root with main.aspx
  trailingSlash: false,
  images: {
    unoptimized: true
  }
};

export default nextConfig;
