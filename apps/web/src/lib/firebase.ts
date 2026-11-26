// File : /team-samsara/apps/web/src/lib/firebase.ts
// Version : 1.2.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Initializes Firebase Auth only — no Firestore or Storage client
// SDK usage anywhere in this app, since all data access routes through the
// C# API. Every value comes from appConfig; the Auth emulator is connected
// only when appConfig provides its URL, which happens in development alone.

import { initializeApp } from "firebase/app";
import { connectAuthEmulator, getAuth } from "firebase/auth";
import { appConfig } from "@/config/appConfig";

const { authEmulatorUrl, ...firebaseConfig } = appConfig.firebase;

const firebaseApp = initializeApp(firebaseConfig);

export const auth = getAuth(firebaseApp);

if (authEmulatorUrl) {
  connectAuthEmulator(auth, authEmulatorUrl, { disableWarnings: true });
}
