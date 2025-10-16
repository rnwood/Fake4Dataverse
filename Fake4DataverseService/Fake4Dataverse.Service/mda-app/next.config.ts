import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Note: For production builds, we export static files to ../src/Fake4Dataverse.Service/wwwroot/mda
  // For development and E2E tests, we use the default .next directory with rewrites for API proxying
  ...(process.env.NODE_ENV === 'production' ? {
    output: 'export',
    distDir: '../src/Fake4Dataverse.Service/wwwroot/mda',
  } : {
    // Proxy API requests to the Fake4Dataverse service on port 5000 (dev/test only)
    async rewrites() {
      return [
        {
          source: '/api/data/:path*',
          destination: 'http://localhost:5000/api/data/:path*',
        },
      ];
    },
  }),
  basePath: '',  // No base path - will serve from root with main.aspx
  trailingSlash: false,
  images: {
    unoptimized: true
  },
};

export default nextConfig;
