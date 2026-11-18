/** @param {{ icon?: import('react').ElementType, title: string, subtitle?: string, footer?: import('react').ReactNode, children?: import('react').ReactNode }} props */
export default function AuthLayout({ icon: Icon, title, subtitle, footer, children }) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4 py-12">
      <div className="w-full max-w-md">
        <section className="rounded-2xl border bg-card p-6 shadow-sm sm:p-8">
          <header className="mb-6 text-center">
            {Icon && (
              <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
                <Icon className="h-6 w-6" aria-hidden="true" />
              </div>
            )}
            <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
            {subtitle && <p className="mt-2 text-sm text-muted-foreground">{subtitle}</p>}
          </header>
          {children}
          {footer && <footer className="mt-6 text-center text-sm">{footer}</footer>}
        </section>
      </div>
    </main>
  );
}
