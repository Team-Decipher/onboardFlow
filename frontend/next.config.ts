import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  cacheComponents: true,
  partialPrefetching: true,
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
  async redirects() {
    return [
      {
        source: "/admin/organisations",
        destination: "/client-organisations",
        permanent: true,
      },
      {
        source: "/reviewer/queue",
        destination: "/review-queue",
        permanent: true,
      },
    ];
  },
};

export default nextConfig;
