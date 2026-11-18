import React, { useState, useEffect, useCallback } from "react";
import { Tag, Sparkles, Truck, ChevronLeft, ChevronRight } from "lucide-react";
import { Image } from "@/components/ui/image";

const SLIDES = [
  {
    icon: Truck,
    title: "Free shipping on orders over ₱3,000",
    subtitle: "Across all the provinces we serve",
    cta: "Start shopping",
    image: "https://media.base44.com/images/public/6a8e324dae5ca7b41e515045/b0180eae5_generated_image.png",
  },
  {
    icon: Tag,
    title: "Feed Sale — 10% off all Animal Feeds",
    subtitle: "Promo runs until end of the month",
    cta: "Shop feeds",
    image: "https://media.base44.com/images/public/6a8e324dae5ca7b41e515045/d36061aa5_generated_image.png",
  },
  {
    icon: Sparkles,
    title: "New: Hybrid corn seeds now in stock",
    subtitle: "High-yield, drought-tolerant varieties",
    cta: "Browse seeds",
    image: "https://media.base44.com/images/public/6a8e324dae5ca7b41e515045/351e576cd_generated_image.png",
  },
];

export default function PromoBanner() {
  const [idx, setIdx] = useState(0);
  const go = useCallback((n) => setIdx((i) => (i + n + SLIDES.length) % SLIDES.length), []);
  useEffect(() => {
    const t = setInterval(() => setIdx((i) => (i + 1) % SLIDES.length), 5000);
    return () => clearInterval(t);
  }, []);
  const slide = SLIDES[idx];
  const Icon = slide.icon;
  return (
    <div className="relative overflow-hidden rounded-2xl bg-emerald-900 text-emerald-50">
      <div className="grid sm:grid-cols-[1fr_40%]">
        <div className="flex flex-col justify-center gap-2 p-4 sm:gap-3 sm:p-8 lg:p-10">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-50/10 sm:h-11 sm:w-11 sm:rounded-xl">
            <Icon className="h-4 w-4 sm:h-5 sm:w-5" />
          </div>
          <h3 className="font-display text-base leading-tight tracking-tight sm:text-2xl lg:text-3xl">
            {slide.title}
          </h3>
          <p className="text-xs text-emerald-100/70 sm:text-base">{slide.subtitle}</p>
          <div>
            <span className="inline-flex items-center rounded-full bg-emerald-50 px-3 py-1.5 text-[11px] font-medium text-emerald-900 sm:px-4 sm:py-2 sm:text-xs">
              {slide.cta}
            </span>
          </div>
        </div>
        <div className="relative hidden h-44 sm:block sm:h-auto">
          <Image src={slide.image} alt="" fittingType="fill" className="h-full w-full object-cover" />
        </div>
      </div>

      <button
        onClick={() => go(-1)}
        aria-label="Previous"
        className="absolute left-2 top-1/2 -translate-y-1/2 rounded-full bg-emerald-950/40 p-1 text-emerald-50 transition hover:bg-emerald-950/60 sm:left-3 sm:p-1.5"
      >
        <ChevronLeft className="h-3.5 w-3.5 sm:h-4 sm:w-4" />
      </button>
      <button
        onClick={() => go(1)}
        aria-label="Next"
        className="absolute right-2 top-1/2 -translate-y-1/2 rounded-full bg-emerald-950/40 p-1 text-emerald-50 transition hover:bg-emerald-950/60 sm:right-3 sm:p-1.5"
      >
        <ChevronRight className="h-3.5 w-3.5 sm:h-4 sm:w-4" />
      </button>

      <div className="absolute bottom-2 left-1/2 flex -translate-x-1/2 gap-1.5 sm:bottom-3">
        {SLIDES.map((_, i) => (
          <button
            key={i}
            onClick={() => setIdx(i)}
            aria-label={`Slide ${i + 1}`}
            className={`h-1.5 rounded-full transition-all ${i === idx ? "w-5 bg-emerald-50" : "w-1.5 bg-emerald-50/40"}`}
          />
        ))}
      </div>
    </div>
  );
}