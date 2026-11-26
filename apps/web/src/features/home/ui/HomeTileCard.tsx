// File : /team-samsara/apps/web/src/features/home/ui/HomeTileCard.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : One tile of the home page: a picture with its kind and title over
// the bottom, and the whole tile leading somewhere. It assembles the shared
// Card parts; the size comes from the grid or row that holds it.

import { assetUrl } from "@/lib/assetUrl";
import { Card } from "@/shared/components/Card/Card";
import { CardContent } from "@/shared/components/Card/CardContent";
import { CardLink } from "@/shared/components/Card/CardLink";
import { CardMedia } from "@/shared/components/Card/CardMedia";
import type { HomeTile } from "../data/homeTiles";
import { HomeTilesText } from "./homeTilesContent";
import styles from "./HomeTileCard.module.css";

interface HomeTileCardProps {
  tile: HomeTile;
  /** Lets the grid or row set the tile's size and position. */
  className?: string;
}

export function HomeTileCard({ tile, className }: HomeTileCardProps) {
  return (
    <Card as="li" className={className}>
      <CardMedia
        src={assetUrl(tile.assetId)}
        alt={tile.alt}
        focus={tile.focus}
      />
      <CardLink
        href={tile.href}
        label={HomeTilesText.tileLinkLabel(tile.label, tile.title)}
      />
      <CardContent className={styles.content}>
        <span className={styles.label}>{tile.label}</span>
        <p className={styles.title}>{tile.title}</p>
      </CardContent>
    </Card>
  );
}
