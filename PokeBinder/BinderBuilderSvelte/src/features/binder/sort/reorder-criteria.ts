/**
 * What a reorder sorts by, as the modal edits it and as the request carries it.
 *
 * Up to three criteria, applied like `ORDER BY a, b, c`: the first orders the whole binder, and
 * each one after it only orders the cards the ones above left level. The sorting itself is the
 * server's -- ReorderBinder -- because what the criteria read (release dates, rarity weights) is
 * the catalog's. Mirrors ReorderBinder's SortCriterion, in the camelCase the serializer reads.
 */

export type CriterionKey = 'set' | 'rarity' | 'name'

/**
 * Which way the underlying value runs. Never shown as is: the modal labels each direction for what
 * it does to that criterion -- see `DIRECTION_LABELS`.
 */
export type SortDirection = 'asc' | 'desc'

/** Which of the catalog's two rarity orderings the Rarity criterion reads. Exclusive by construction. */
export type RarityKey = 'pullRate' | 'name'

/** Only Rarity carries a `rarityKey`; the server refuses one on anything else. */
export type SortCriterion =
  | { key: 'set'; direction: SortDirection }
  | { key: 'rarity'; direction: SortDirection; rarityKey: RarityKey }
  | { key: 'name'; direction: SortDirection }

export const CRITERION_NAMES: Record<CriterionKey, string> = {
  set: 'Set',
  rarity: 'Rarity',
  name: 'Card name',
}

/**
 * Each criterion's two directions, in words about the cards rather than about the number, and in
 * the order the modal offers them.
 *
 * Both rarity columns are numbered so that bigger means rarer, so "rare first" is the descending
 * end whichever key the criterion reads -- which is what lets one pair of labels sit over both.
 * Release dates grow with time, so "newest first" is descending too.
 */
export const DIRECTION_LABELS: Record<CriterionKey, Record<SortDirection, string>> = {
  set: { desc: 'Newest first', asc: 'Oldest first' },
  rarity: { desc: 'Rare first', asc: 'Common first' },
  name: { asc: 'A–Z', desc: 'Z–A' },
}

export const RARITY_KEY_LABELS: Record<RarityKey, string> = {
  pullRate: 'Pull rate',
  name: 'Name order',
}

/** What the modal opens on before anything has been applied: every criterion, in the listed order. */
export function defaultCriteria(): SortCriterion[] {
  return [
    { key: 'set', direction: 'desc' },
    { key: 'rarity', direction: 'desc', rarityKey: 'pullRate' },
    { key: 'name', direction: 'asc' },
  ]
}

/** A fresh criterion of one kind, for putting a dropped one back. Same directions as the defaults. */
export function newCriterion(key: CriterionKey): SortCriterion {
  return defaultCriteria().find((criterion) => criterion.key === key)!
}
