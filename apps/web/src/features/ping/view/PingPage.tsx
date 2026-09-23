// File : /team-samsara/apps/web/src/features/ping/view/PingPage.tsx
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Renders the result of usePing. This is the frontend's proof
// point — same role as the backend's PingPongModule — confirming the router,
// the API client, the Query setup, and the Vite dev proxy all connect end to
// end. Deleted once a real feature exists.

import { usePing } from "../service/usePing";
import styles from "../styling/PingPage.module.css";

export function PingPage() {
  const { data, isLoading, isError, error } = usePing();

  return (
    <div className={styles.container}>
      <h1>Ping</h1>
      {isLoading && <p>Loading...</p>}
      {isError && <p className={styles.result}>Error: {error.message}</p>}
      {data && <p className={styles.result}>{data}</p>}
    </div>
  );
}
