import styles from './TypingCursor.module.css';

/**
 * Atom — animated blinking cursor `|` shown while the LLM is streaming.
 *
 * Rendered inline so it appears right after the last token in
 * `StreamingMessage`. Disappears once `visible` becomes false.
 */
export function TypingCursor({ visible }: { visible: boolean }) {
    if (!visible) return null;
    return <span className={styles.cursor} aria-hidden="true" />;
}
