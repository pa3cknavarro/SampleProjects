// Renders the project brand mark shared by storefront navigation and panels.
export default function BrandLogo({ size = 'md', className = '' }) {
  // Selects a compact title size for small variants and a larger title otherwise.
  const textSize = size === 'sm' ? 'text-lg' : 'text-xl sm:text-2xl'

  // Returns an accessible text-based mark that does not depend on external image assets.
  return (
    // Keeps the brand wordmark aligned with surrounding flex layouts.
    <span className={`font-semibold tracking-tight text-emerald-950 ${textSize} ${className}`}>
      {/* Provides the stable brand label used throughout the storefront. */}
      CVAEC
    </span>
  )
}