// File : /team-samsara/apps/web/src/features/account/view/SecurityOverviewPage.tsx
// Version : 1.1.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The account security overview at /account/security: the sign-in email and
// one card each for changing the password or email, signing out everywhere and deleting
// the account. Password and email open their own page; the other two open a modal.

import { useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { routePaths } from "@/shared/constants/routePaths";
import { DeleteAccountModal } from "./DeleteAccountModal";
import { LogOutEverywhereModal } from "./LogOutEverywhereModal";
import styles from "./SecurityOverviewPage.module.css";
import { SecurityOverviewText } from "./securityOverviewContent";

type OpenModal = "sessions" | "delete" | null;

export function SecurityOverviewPage() {
  const { user } = useAuth();
  const [openModal, setOpenModal] = useState<OpenModal>(null);

  return (
    <main className={styles.page}>
      <Link to={routePaths.accountProfile} className={styles.back}>
        {SecurityOverviewText.back}
      </Link>
      <h1 className={styles.title}>{SecurityOverviewText.title}</h1>

      <div className={styles.cards}>
        <section className={styles.card} aria-labelledby="security-password">
          <div className={styles.cardText}>
            <h2 id="security-password" className={styles.cardTitle}>
              {SecurityOverviewText.passwordTitle}
            </h2>
            <p className={styles.cardBody}>
              {SecurityOverviewText.passwordBody}
            </p>
          </div>
          <Link
            to={routePaths.accountSecurityPassword}
            className={styles.action}
          >
            {SecurityOverviewText.passwordAction}
          </Link>
        </section>

        <section className={styles.card} aria-labelledby="security-email">
          <div className={styles.cardText}>
            <h2 id="security-email" className={styles.cardTitle}>
              {SecurityOverviewText.emailTitle}
            </h2>
            {user?.email && <p className={styles.email}>{user.email}</p>}
            <p className={styles.cardBody}>{SecurityOverviewText.emailBody}</p>
          </div>
          <Link to={routePaths.accountSecurityEmail} className={styles.action}>
            {SecurityOverviewText.emailAction}
          </Link>
        </section>

        <section className={styles.card} aria-labelledby="security-sessions">
          <div className={styles.cardText}>
            <h2 id="security-sessions" className={styles.cardTitle}>
              {SecurityOverviewText.sessionsTitle}
            </h2>
            <p className={styles.cardBody}>
              {SecurityOverviewText.sessionsBody}
            </p>
          </div>
          <button
            type="button"
            className={styles.action}
            onClick={() => setOpenModal("sessions")}
          >
            {SecurityOverviewText.sessionsAction}
          </button>
        </section>

        <section
          className={`${styles.card} ${styles.danger}`}
          aria-labelledby="security-delete"
        >
          <div className={styles.cardText}>
            <h2 id="security-delete" className={styles.cardTitle}>
              {SecurityOverviewText.deleteTitle}
            </h2>
            <p className={styles.cardBody}>{SecurityOverviewText.deleteBody}</p>
          </div>
          <button
            type="button"
            className={styles.action}
            onClick={() => setOpenModal("delete")}
          >
            {SecurityOverviewText.deleteAction}
          </button>
        </section>
      </div>

      {openModal === "sessions" ? (
        <LogOutEverywhereModal onClose={() => setOpenModal(null)} />
      ) : null}
      {openModal === "delete" ? (
        <DeleteAccountModal onClose={() => setOpenModal(null)} />
      ) : null}
    </main>
  );
}
