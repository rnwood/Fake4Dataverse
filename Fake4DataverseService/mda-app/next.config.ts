import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'export',
  distDir: '../src/Fake4Dataverse.Service/wwwroot/mda',
  basePath: '/mda',
  trailingSlash: true,
  images: {
    unoptimized: true
  }
};

export default nextConfig;
