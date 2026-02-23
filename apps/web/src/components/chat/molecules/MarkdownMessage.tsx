'use client';

import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';
import type { ChatMessage } from '@hydroespinaca/shared';
import { ChatBubbleWeb } from '../atoms/ChatBubbleWeb';
import styles from './MarkdownMessage.module.css';

interface MarkdownMessageProps {
    message: ChatMessage;
}

/**
 * Molecule — renders a completed model message with full Markdown support.
 *
 * Composes `ChatBubbleWeb` + `react-markdown` + `react-syntax-highlighter`.
 * Used for all finalised messages stored in the session history.
 * During streaming use `StreamingMessage` instead.
 */
export function MarkdownMessage({ message }: MarkdownMessageProps) {
    if (message.role === 'user') {
        return (
            <ChatBubbleWeb role="user">
                <span className={styles.userText}>{message.content}</span>
            </ChatBubbleWeb>
        );
    }

    if (message.role === 'system') {
        return (
            <ChatBubbleWeb role="system">
                <span>{message.content}</span>
            </ChatBubbleWeb>
        );
    }

    return (
        <ChatBubbleWeb role="model">
            <ReactMarkdown
                remarkPlugins={[remarkGfm]}
                className={styles.markdown}
                components={{
                    // Syntax-highlighted code blocks
                    code({ node: _node, className, children, ...props }) {
                        const match = /language-(\w+)/.exec(className || '');
                        const isBlock = match !== null;
                        return isBlock ? (
                            <SyntaxHighlighter
                                style={oneDark}
                                language={match[1]}
                                PreTag="div"
                                customStyle={{
                                    margin: '8px 0',
                                    borderRadius: '8px',
                                    fontSize: '0.82rem',
                                }}
                            >
                                {String(children).replace(/\n$/, '')}
                            </SyntaxHighlighter>
                        ) : (
                            <code className={styles.inlineCode} {...props}>
                                {children}
                            </code>
                        );
                    },
                    // Open links in a new tab
                    a({ children, href, ...props }) {
                        return (
                            <a href={href} target="_blank" rel="noopener noreferrer" {...props}>
                                {children}
                            </a>
                        );
                    },
                }}
            >
                {message.content}
            </ReactMarkdown>
        </ChatBubbleWeb>
    );
}
