import type { Locale } from "../types";
import ar from "./ar.json";
import en from "./en.json";
import type { Translation } from "./types";

export const copy: Record<Locale, Translation> = { ar, en };
export type { Translation } from "./types";
export function translationFor(locale: Locale) {
  return copy[locale];
}
