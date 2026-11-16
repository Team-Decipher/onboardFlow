import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const token = request.cookies.get('onboardflow_token')?.value;
  const role = request.cookies.get('onboardflow_role')?.value;

  const isAuthRoute = pathname.startsWith('/login');
  const isProtectedWorkspace =
    pathname.startsWith('/admin') ||
    pathname.startsWith('/dashboard') ||
    pathname.startsWith('/manager') ||
    pathname.startsWith('/reviewer') ||
    pathname.startsWith('/client');

  // 1. Unauthenticated users visiting protected pages -> Redirect to /login
  if (isProtectedWorkspace && !token) {
    const loginUrl = new URL('/login', request.url);
    return NextResponse.redirect(loginUrl);
  }

  // 2. Already authenticated users visiting login -> Redirect to /dashboard
  if (isAuthRoute && token) {
    const dashboardUrl = new URL('/dashboard', request.url);
    return NextResponse.redirect(dashboardUrl);
  }

  // 3. Role-based Route Enforcement for Admin area
  if (pathname.startsWith('/admin')) {
    if (role !== 'Administrator') {
      const unauthorizedUrl = new URL('/unauthorized', request.url);
      return NextResponse.redirect(unauthorizedUrl);
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    '/admin/:path*',
    '/dashboard/:path*',
    '/manager/:path*',
    '/reviewer/:path*',
    '/client/:path*',
    '/login',
  ],
};

