/**
 * Common product tags for autocomplete in ProductNotes component
 */
export const COMMON_PRODUCT_TAGS = [
    'organic',
    'gluten-free',
    'dairy-free',
    'vegan',
    'vegetarian',
    'low-sodium',
    'sugar-free',
    'keto-friendly',
    'paleo',
    'whole-grain',
    'non-GMO',
    'fair-trade',
    'local',
    'seasonal',
    'frozen',
    'fresh',
    'canned',
    'bulk',
    'favorite',
    'sale-item',
    'try-new',
    'family-approved'
] as const;

export type ProductTag = typeof COMMON_PRODUCT_TAGS[number];
