import React from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Share, Plus, MoreVertical, Download, Check } from 'lucide-react';

function Step({ num, children }) {
  return (
    <div className="flex items-start gap-3">
      <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-xs font-semibold text-emerald-900">
        {num}
      </div>
      <p className="flex-1 pt-0.5 text-sm leading-relaxed text-emerald-950/80">{children}</p>
    </div>
  );
}

export default function InstallPrompt({
  open,
  onOpenChange,
  platform,
  canInstallNatively,
  onNativeInstall,
}) {
  const isIOS = platform === 'ios';
  const isAndroid = platform === 'android';

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md rounded-2xl bg-stone-50 p-0 sm:p-0">
        <DialogHeader className="space-y-1 p-6 pb-2">
          <DialogTitle className="font-display text-xl text-emerald-950">
            Install CVAEC
          </DialogTitle>
          <DialogDescription className="text-sm text-emerald-900/60">
            Add CVAEC to your Home Screen for faster access.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 px-6 pb-6">
          {canInstallNatively && (
            <Button
              onClick={onNativeInstall}
              className="w-full gap-2 rounded-full bg-emerald-900 py-2.5 text-sm font-medium text-emerald-50 hover:bg-emerald-800"
            >
              <Download className="h-4 w-4" />
              Install Now
            </Button>
          )}

          {canInstallNatively && (
            <p className="text-center text-xs text-emerald-900/40">
              or follow the manual steps below
            </p>
          )}

          <div className="space-y-3 rounded-xl bg-white p-4">
            {isIOS && (
              <>
                <Step num={1}>
                  Tap the <Share className="inline h-4 w-4 align-text-bottom text-emerald-700" /> Share button at the bottom of the browser.
                </Step>
                <Step num={2}>
                  Scroll the share sheet and select <strong>&ldquo;Add to Home Screen&rdquo;</strong>.
                </Step>
                <Step num={3}>
                  Tap <strong>&ldquo;Add&rdquo;</strong> to confirm.
                </Step>
              </>
            )}
            {isAndroid && (
              <>
                <Step num={1}>
                  Tap the <MoreVertical className="inline h-4 w-4 align-text-bottom text-emerald-700" /> menu button (three dots) in the browser toolbar.
                </Step>
                <Step num={2}>
                  Select <strong>&ldquo;Add to Home screen&rdquo;</strong>.
                </Step>
                <Step num={3}>
                  Tap <strong>&ldquo;Add&rdquo;</strong> to confirm.
                </Step>
              </>
            )}
            {!isIOS && !isAndroid && (
              <>
                <Step num={1}>
                  Click the <Download className="inline h-4 w-4 align-text-bottom text-emerald-700" /> install icon in the address bar, or open the browser menu and select <strong>&ldquo;Install CVAEC&rdquo;</strong>.
                </Step>
                <Step num={2}>
                  Click <strong>&ldquo;Install&rdquo;</strong> to confirm.
                </Step>
              </>
            )}
          </div>

          <div className="flex items-center gap-2 rounded-lg bg-emerald-50 px-3 py-2 text-xs text-emerald-800/70">
            <Check className="h-3.5 w-3.5 shrink-0" />
            Works offline after installation — browse products anytime.
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}