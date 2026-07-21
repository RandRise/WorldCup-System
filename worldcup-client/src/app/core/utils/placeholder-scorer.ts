/** Import/sync fallback player name — never show as a real scorer in UI. */
export const PLACEHOLDER_SCORER_NAME = 'Tournament Scorer';

/**
 * Returns null when the name is the Tournament Scorer placeholder
 * so Recent events / timelines show team + event type only.
 */
export function toHonestPlayerName(name?: string | null): string | null {
  if (name == null) {
    return null;
  }
  const trimmed = name.trim();
  if (!trimmed || trimmed === PLACEHOLDER_SCORER_NAME) {
    return null;
  }
  return name;
}
