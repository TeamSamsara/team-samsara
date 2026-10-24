// File : /team-samsara/apps/web/src/shared/icons/iconRegistry.ts
// Version : 1.2.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : The only file that knows where each icon comes from; screens use the names.

import type { ComponentType } from "react";
import {
  Bell,
  Calendar,
  Camera,
  Check,
  ChevronDown,
  CircleAlert,
  Eye,
  EyeOff,
  Image,
  Info,
  KeyRound,
  Lock,
  LogOut,
  Mail,
  Pencil,
  Settings,
  ShieldCheck,
  Trash2,
  TriangleAlert,
  Upload,
  User,
  UserPen,
  X,
} from "lucide-react";
import {
  SiDiscord,
  SiInstagram,
  SiTiktok,
  SiTwitch,
  SiX,
  SiYoutube,
} from "react-icons/si";

export interface IconProps {
  className?: string;
  strokeWidth?: number;
  "aria-hidden"?: true;
}

type IconComponent = ComponentType<IconProps>;

// Names say what the icon means, not what it looks like, so a design change
// is one line here. Add new icons in the group that fits.
export const iconRegistry = {
  // Actions
  check: Check,
  close: X,
  edit: Pencil,
  upload: Upload,
  trash: Trash2,
  logOut: LogOut,

  // Navigation and state
  chevronDown: ChevronDown,
  info: Info,
  warning: TriangleAlert,
  error: CircleAlert,
  notifications: Bell,

  // Account
  user: User,
  profileSettings: UserPen,
  settings: Settings,
  security: ShieldCheck,
  password: KeyRound,
  lock: Lock,
  email: Mail,
  showPassword: Eye,
  hidePassword: EyeOff,

  // Content
  calendar: Calendar,
  camera: Camera,
  image: Image,

  // Brands (for the socials)
  discord: SiDiscord,
  instagram: SiInstagram,
  tiktok: SiTiktok,
  twitch: SiTwitch,
  x: SiX,
  youtube: SiYoutube,
} as const satisfies Record<string, IconComponent>;

export type IconName = keyof typeof iconRegistry;
