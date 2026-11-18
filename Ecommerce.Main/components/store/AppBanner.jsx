import React, { useState } from 'react';
import { Leaf, Download, X } from 'lucide-react';
import { usePWAInstall } from '@/hooks/usePWAInstall';
import InstallPrompt from './InstallPrompt';

const DISMISS_KEY = 'farmfur-install-dismissed';

export default function AppBanner() {
  const { isInstalled, platform, canInstallNatively, promptInstall } = usePWAInstall();
  const [dismissed, setDismissed] = useState(() => {
    try {
      return localStorage.getItem(DISMISS_KEY) === 'true';
    } catch {
      return false;
    }
  });
  const [showInstructions, setShowInstructions] = useState(false);

  if (isInstalled || dismissed) return null;

  const handleInstall = async () => {
    if (canInstallNatively) {
      await promptInstall();
    } else {
      setShowInstructions(true);
    }
  };

  const handleDismiss = () => {
    try {
      localStorage.setItem(DISMISS_KEY, 'true');
    } catch {
      // ignore
    }
    setDismissed(true);
  };

  return (
    <>
      <div className="relative z-40 bg-emerald-950 text-emerald-50">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-3 px-5 py-2.5 lg:px-10">
          <div className="flex min-w-0 items-center gap-2.5">
            <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-emerald-50/10">
              <Leaf className="h-3.5 w-3.5" />
            </div>
            <p className="truncate text-[11px] tracking-wide text-emerald-50/90 sm:text-xs">
              Get the CVAEC app for a faster, offline-ready shopping experience.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={handleInstall}
              className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-3 py-1.5 text-xs font-medium text-emerald-950 transition hover:bg-white"
            >
              <Download className="h-3.5 w-3.5" />
              Install
            </button>
            <button
              onClick={handleDismiss}
              className="rounded-full p-1.5 text-emerald-50/70 transition hover:bg-emerald-50/10 hover:text-emerald-50"
              aria-label="Dismiss"
            >
              <X className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>
      <InstallPrompt
        open={showInstructions}
        onOpenChange={setShowInstructions}
        platform={platform}
        canInstallNatively={canInstallNatively}
        onNativeInstall={async () => {
          const accepted = await promptInstall();
          if (accepted) setShowInstructions(false);
        }}
      />
    </>
  );
}