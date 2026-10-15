// File : /team-samsara/apps/web/src/shared/components/Card/CardLink.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Makes a whole card a link. It covers the card and opens the right
// way for where it leads: a page of this site through the router, another
// website in a new tab, anything else (email, phone) as a plain link. Buttons
// and links placed inside the card's content stay clickable on top of it.

import type { AnchorHTMLAttributes } from "react";
import { Link } from "react-router-dom";
import { getHrefKind } from "@/lib/hrefKind";
import { CardText } from "./cardText";
import styles from "./Card.module.css";

interface CardLinkProps extends Omit<
  AnchorHTMLAttributes<HTMLAnchorElement>,
  "href" | "children"
> {
  /** Where the card leads: a path on this site, or a full address elsewhere. */
  href: string;
  /** What screen readers announce for the card, for example its title. */
  label: string;
}

export function CardLink({
  href,
  label,
  className,
  ...anchorProps
}: CardLinkProps) {
  const origin = window.location.origin;
  const kind = getHrefKind(href, origin);
  const classes = className ? `${styles.link} ${className}` : styles.link;

  const text = (
    <span className={styles.hidden}>
      {kind === "external" ? `${label} ${CardText.opensInNewTab}` : label}
    </span>
  );

  if (kind === "internal") {
    const url = new URL(href, origin);
    return (
      <Link
        {...anchorProps}
        to={`${url.pathname}${url.search}${url.hash}`}
        className={classes}
      >
        {text}
      </Link>
    );
  }

  if (kind === "external") {
    return (
      <a
        {...anchorProps}
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        className={classes}
      >
        {text}
      </a>
    );
  }

  return (
    <a {...anchorProps} href={href} className={classes}>
      {text}
    </a>
  );
}
