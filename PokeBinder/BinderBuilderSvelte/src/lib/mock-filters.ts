// Mock filter data — placeholder until the CardSearch slices back these fields.

export interface FilterOption {
  label: string
  value: string
}

export const generations: FilterOption[] = [
  { label: 'Generation I', value: 'gen-1' },
  { label: 'Generation II', value: 'gen-2' },
  { label: 'Generation III', value: 'gen-3' },
  { label: 'Generation IV', value: 'gen-4' },
  { label: 'Generation V', value: 'gen-5' },
  { label: 'Generation VI', value: 'gen-6' },
  { label: 'Generation VII', value: 'gen-7' },
  { label: 'Generation VIII', value: 'gen-8' },
  { label: 'Generation IX', value: 'gen-9' },
]

export const sets: FilterOption[] = [
  { label: 'Base Set', value: 'base-set' },
  { label: 'Jungle', value: 'jungle' },
  { label: 'Fossil', value: 'fossil' },
  { label: 'Team Rocket', value: 'team-rocket' },
  { label: 'Neo Genesis', value: 'neo-genesis' },
  { label: 'Perfect Order', value: 'perfect-order' },
  { label: 'Paldea Evolved', value: 'paldea-evolved' },
  { label: 'Surging Sparks', value: 'surging-sparks' },
]

export const pokemon: FilterOption[] = [
  { label: 'Bulbasaur', value: 'bulbasaur' },
  { label: 'Charmander', value: 'charmander' },
  { label: 'Squirtle', value: 'squirtle' },
  { label: 'Pikachu', value: 'pikachu' },
  { label: 'Eevee', value: 'eevee' },
  { label: 'Snivy', value: 'snivy' },
  { label: 'Rowlet', value: 'rowlet' },
  { label: 'Vivillon', value: 'vivillon' },
]

export const rarities: FilterOption[] = [
  { label: 'Common', value: 'common' },
  { label: 'Uncommon', value: 'uncommon' },
  { label: 'Rare', value: 'rare' },
  { label: 'Holo Rare', value: 'holo-rare' },
  { label: 'Ultra Rare', value: 'ultra-rare' },
  { label: 'Secret Rare', value: 'secret-rare' },
]

export const cardTypes: FilterOption[] = [
  { label: 'Grass', value: 'grass' },
  { label: 'Fire', value: 'fire' },
  { label: 'Water', value: 'water' },
  { label: 'Lightning', value: 'lightning' },
  { label: 'Psychic', value: 'psychic' },
  { label: 'Fighting', value: 'fighting' },
  { label: 'Darkness', value: 'darkness' },
  { label: 'Metal', value: 'metal' },
  { label: 'Fairy', value: 'fairy' },
  { label: 'Dragon', value: 'dragon' },
  { label: 'Colorless', value: 'colorless' },
]
