// File : /team-samsara/apps/web/src/features/contact/view/ContactPage.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The public contact page: a form that sends a message to the team, with an optional
// phone number and attached files. Anyone can use it; the API limits how often one address can.

import { TextArea } from "@/shared/components/Fields/TextArea/TextArea";
import { TextField } from "@/shared/components/Fields/TextField/TextField";
import { InlineMessage } from "@/shared/components/InlineMessage/InlineMessage";
import {
  ContactFormFields,
  contactLimits,
} from "../constants/contactConstants";
import { useContactForm } from "../hooks/useContactForm";
import { AttachFiles } from "../ui/AttachFiles";
import { SubjectSelect } from "../ui/SubjectSelect";
import { ContactPageText } from "./contactPageContent";
import styles from "./ContactPage.module.css";

export function ContactPage() {
  const form = useContactForm();
  const { values, errors } = form;

  return (
    <main className={styles.page}>
      <h1 className={styles.title}>{ContactPageText.title}</h1>

      <form
        className={styles.form}
        onSubmit={(event) => void form.submit(event)}
        noValidate
      >
        <div className={styles.grid}>
          <TextField
            label={ContactPageText.firstName}
            placeholder={ContactPageText.firstName}
            name={ContactFormFields.firstName}
            autoComplete="given-name"
            maxLength={contactLimits.maxNameLength}
            value={values.firstName}
            error={errors.firstName}
            onChange={(event) =>
              form.changeField("firstName", event.target.value)
            }
            onBlur={() => form.blurField("firstName")}
          />
          <TextField
            label={ContactPageText.lastName}
            placeholder={ContactPageText.lastName}
            name={ContactFormFields.lastName}
            autoComplete="family-name"
            maxLength={contactLimits.maxNameLength}
            value={values.lastName}
            error={errors.lastName}
            onChange={(event) =>
              form.changeField("lastName", event.target.value)
            }
            onBlur={() => form.blurField("lastName")}
          />
          <TextField
            label={ContactPageText.phone}
            placeholder={ContactPageText.phone}
            name={ContactFormFields.phone}
            type="tel"
            autoComplete="tel"
            maxLength={contactLimits.maxPhoneLength}
            value={values.phone}
            error={errors.phone}
            onChange={(event) => form.changeField("phone", event.target.value)}
            onBlur={() => form.blurField("phone")}
          />
          <TextField
            label={ContactPageText.email}
            placeholder={ContactPageText.email}
            name={ContactFormFields.email}
            type="email"
            autoComplete="email"
            value={values.email}
            error={errors.email}
            onChange={(event) => form.changeField("email", event.target.value)}
            onBlur={() => form.blurField("email")}
          />
        </div>

        <div className={styles.subject}>
          <SubjectSelect
            label={ContactPageText.subject}
            value={values.category}
            error={errors.category}
            onChange={(value) => form.changeField("category", value)}
            onBlur={() => form.blurField("category")}
          />
        </div>

        <TextArea
          label={ContactPageText.message}
          placeholder={ContactPageText.message}
          name={ContactFormFields.message}
          maxLength={contactLimits.maxMessageLength}
          value={values.message}
          error={errors.message}
          onChange={(event) => form.changeField("message", event.target.value)}
          onBlur={() => form.blurField("message")}
        />

        {/* A trap for bots: people never see it, a form-filling script fills it. */}
        <div className={styles.trap} aria-hidden="true">
          <label>
            {ContactPageText.honeypot}
            <input
              type="text"
              name={ContactFormFields.honeypot}
              tabIndex={-1}
              autoComplete="off"
              value={values.honeypot}
              onChange={(event) =>
                form.changeField("honeypot", event.target.value)
              }
            />
          </label>
        </div>

        <div className={styles.actions}>
          <div className={styles.attach}>
            <AttachFiles
              files={values.files}
              error={errors.files}
              onAdd={form.addFiles}
              onRemove={form.removeFile}
            />
          </div>

          <button
            type="submit"
            className={styles.send}
            disabled={form.isSubmitting}
            aria-busy={form.isSubmitting || undefined}
          >
            {ContactPageText.send}
          </button>
        </div>

        <InlineMessage
          message={form.formError}
          withIcon
          className={styles.formError}
        />
      </form>
    </main>
  );
}
