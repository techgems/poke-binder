/** A single selectable option in a filter field. */
export interface FilterOption {
  label: string
  value: string
}

/** A card type option, shown as its energy symbol rather than its name. */
export interface CardTypeOption extends FilterOption {
  /** Symbol art for the type, or null when there is none and the name is shown instead. */
  imageUrl: string | null
}
