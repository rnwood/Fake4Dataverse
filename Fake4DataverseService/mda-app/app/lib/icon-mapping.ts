/**
 * Icon mapping utilities for converting entity metadata icons to Fluent UI icons
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/create-site-map-app
 * 
 * Maps MDI (Material Design Icons) style icon names to Fluent UI React Icons
 * Reference: https://react.fluentui.dev/?path=/docs/icons-catalog--docs
 */

import {
  Money20Regular,
  MoneyHand20Regular,
  Building20Regular,
  Person20Regular,
  PersonStar20Regular,
  PersonSupport20Regular,
  Tag20Regular,
  BookOpen20Regular,
  Home20Regular,
  Calendar20Regular,
  Mail20Regular,
  Phone20Regular,
  Briefcase20Regular,
  Clipboard20Regular,
  DataBarVertical20Regular,
  Settings20Regular,
  Folder20Regular,
  Document20Regular,
  Circle20Regular,
} from '@fluentui/react-icons';

// Type for Fluent UI icon components
type FluentIconComponent = React.ComponentType<{ className?: string }>;

/**
 * Icon mapping from MDI-style names to Fluent UI icon components
 * These mappings represent common entity icons used in Dynamics 365 / Power Apps
 */
const ICON_MAP: Record<string, FluentIconComponent> = {
  // Sales related
  'mdi-cash': Money20Regular,
  'mdi-currency-usd': MoneyHand20Regular,
  'mdi-domain': Building20Regular,
  'mdi-account': Person20Regular,
  'mdi-account-star': PersonStar20Regular,
  
  // Service related
  'mdi-lifebuoy': PersonSupport20Regular,
  'mdi-ticket': Tag20Regular,
  'mdi-book-open-variant': BookOpen20Regular,
  
  // Common entity types
  'mdi-home': Home20Regular,
  'mdi-calendar': Calendar20Regular,
  'mdi-email': Mail20Regular,
  'mdi-phone': Phone20Regular,
  'mdi-briefcase': Briefcase20Regular,
  'mdi-clipboard': Clipboard20Regular,
  'mdi-chart-bar': DataBarVertical20Regular,
  'mdi-cog': Settings20Regular,
  'mdi-folder': Folder20Regular,
  'mdi-file': Document20Regular,
  
  // Default fallback
  'default': Circle20Regular,
};

/**
 * Get Fluent UI icon component from icon name
 * @param iconName Icon name from entity metadata (e.g., "mdi-cash")
 * @returns Fluent UI icon component or null if not found
 */
export function getIconComponent(iconName?: string): FluentIconComponent | null {
  if (!iconName) {
    return null;
  }

  // Map from MDI-style name to Fluent UI component
  return ICON_MAP[iconName] || null;
}

/**
 * Get default icon component for entities
 */
export function getDefaultIcon(): FluentIconComponent {
  return Circle20Regular;
}
