/**
 * Icon mapping utilities for converting entity metadata icons to Fluent UI icons
 * Reference: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/create-site-map-app
 * 
 * Maps MDI (Material Design Icons) style icon names to Fluent UI React Icons
 * Reference: https://react.fluentui.dev/?path=/docs/icons-catalog--docs
 */

import * as FluentIcons from '@fluentui/react-icons';

// Type for Fluent UI icon components
type FluentIconComponent = React.ComponentType<{ className?: string }>;

/**
 * Icon mapping from MDI-style names to Fluent UI icon names
 * These mappings represent common entity icons used in Dynamics 365 / Power Apps
 */
const ICON_MAP: Record<string, string> = {
  // Sales related
  'mdi-cash': 'Money20Regular',
  'mdi-currency-usd': 'MoneyHand20Regular',
  'mdi-domain': 'Building20Regular',
  'mdi-account': 'Person20Regular',
  'mdi-account-star': 'PersonStar20Regular',
  
  // Service related
  'mdi-lifebuoy': 'PersonSupport20Regular',
  'mdi-ticket': 'Ticket20Regular',
  'mdi-book-open-variant': 'BookOpen20Regular',
  
  // Common entity types
  'mdi-home': 'Home20Regular',
  'mdi-calendar': 'Calendar20Regular',
  'mdi-email': 'Mail20Regular',
  'mdi-phone': 'Phone20Regular',
  'mdi-briefcase': 'Briefcase20Regular',
  'mdi-clipboard': 'Clipboard20Regular',
  'mdi-chart-bar': 'DataBarVertical20Regular',
  'mdi-cog': 'Settings20Regular',
  'mdi-folder': 'Folder20Regular',
  'mdi-file': 'Document20Regular',
  
  // Default fallback
  'default': 'Circle20Regular',
};

/**
 * Get Fluent UI icon component from icon name
 * @param iconName Icon name from entity metadata (e.g., "mdi-cash" or "Money20Regular")
 * @returns Fluent UI icon component or null if not found
 */
export function getIconComponent(iconName?: string): FluentIconComponent | null {
  if (!iconName) {
    return null;
  }

  // If it's already a Fluent UI icon name, try to use it directly
  if (iconName.endsWith('Regular') || iconName.endsWith('Filled')) {
    const iconComponent = (FluentIcons as any)[iconName];
    if (iconComponent) {
      return iconComponent;
    }
  }

  // Map from MDI-style name to Fluent UI name
  const fluentIconName = ICON_MAP[iconName] || ICON_MAP['default'];
  const iconComponent = (FluentIcons as any)[fluentIconName];
  
  return iconComponent || null;
}

/**
 * Get default icon component for entities
 */
export function getDefaultIcon(): FluentIconComponent {
  return (FluentIcons as any)['Circle20Regular'];
}
