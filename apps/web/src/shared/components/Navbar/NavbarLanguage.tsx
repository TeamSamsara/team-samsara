// File : /team-samsara/apps/web/src/shared/components/Navbar/NavbarLanguage.tsx
// Version : 1.1.0
// Latest commit: task/web-icons
// Author : Gerrah
// Purpose : The language switcher in the navbar: a button showing the current
// language that opens a list of the available ones. It assembles the shared
// Dropdown parts; the choice itself is kept by useLanguage.

import { Dropdown } from "@/shared/components/Dropdown/Dropdown";
import { DropdownItem } from "@/shared/components/Dropdown/DropdownItem";
import { DropdownPanel } from "@/shared/components/Dropdown/DropdownPanel";
import { DropdownTrigger } from "@/shared/components/Dropdown/DropdownTrigger";
import { supportedLanguages } from "@/shared/constants/languages";
import { useLanguage } from "@/shared/hooks/useLanguage";
import { Icon } from "@/shared/icons/Icon";
import { NavbarText } from "./navbarContent";
import styles from "./Navbar.module.css";

export function NavbarLanguage() {
  const { language, setLanguage } = useLanguage();
  const active = supportedLanguages.find((item) => item.code === language);

  return (
    <Dropdown>
      <DropdownTrigger
        popup="listbox"
        className={styles.language}
        aria-label={NavbarText.changeLanguage}
      >
        {active?.label}
        <Icon name="chevronDown" className={styles.chevron} strokeWidth={2.6} />
      </DropdownTrigger>
      <DropdownPanel
        role="listbox"
        aria-label={NavbarText.languageMenu}
        placement="bottom-end"
        offset={14}
        className={styles.languagePanel}
      >
        {supportedLanguages.map((item) => (
          <DropdownItem
            key={item.code}
            role="option"
            lang={item.code}
            aria-label={item.name}
            selected={item.code === language}
            onSelect={() => setLanguage(item.code)}
            className={styles.languageItem}
          >
            {item.label}
          </DropdownItem>
        ))}
      </DropdownPanel>
    </Dropdown>
  );
}
