(function ($) {
    "use strict";

    // Ensure jQuery is present
    if (typeof $ === 'undefined') {
        console.error('chat.js requires jQuery');
        return;
    }

    // Configuration
    const INPUT_SELECTOR = '#chatInput, #floating-chat-input, textarea.chat-input, input.chat-input';
    const MESSAGES_CONTAINER = '#chatMessages';
    const SEND_BUTTON_SELECTOR = '[data-chat-send], .chat-send, #chatSendBtn, button.send-chat';
    // Extra selectors specifically targeting floating input send widgets
    const FLOATING_SEND_SELECTORS = "[data-target='#floating-chat-input'], [data-chat-target='#floating-chat-input'], #floating-chat-send, .send-floating, [data-send-target='#floating-chat-input']";
    const ARIA_LIVE_ID = 'chat-aria-live';
    const MAX_RETRIES = 3;
    const RETRY_BASE_MS = 500; // exponential backoff base
    const SEND_THROTTLE_MS = 500; // avoid double-click rapid sends

    // Internal state
    const sendingMap = new WeakMap(); // DOM element -> boolean
    const lastSendTs = new WeakMap();

    // Ensure an ARIA live region exists for accessible notifications
    function ensureAriaLive() {
        let node = document.getElementById(ARIA_LIVE_ID);
        if (!node) {
            node = document.createElement('div');
            node.id = ARIA_LIVE_ID;
            node.setAttribute('aria-live', 'polite');
            node.setAttribute('aria-atomic', 'true');
            node.style.position = 'absolute';
            node.style.left = '-9999px';
            node.style.width = '1px';
            node.style.height = '1px';
            document.body.appendChild(node);
        }
        return $(node);
    }

    const $ariaLive = ensureAriaLive();

    // Accessible notification + visual system message
    function notify(message, type = 'danger') {
        try {
            if (!message) return;
            // ARIA
            $ariaLive.text(message);
            // visual
            appendSystemMessage(message, type);
            // console
            console[type === 'danger' ? 'error' : 'info']('chat.notify: ', message);
        } catch (ex) {
            console.error('notify error', ex, message);
        }
    }

    // Inline error next to input, with optional retry callback
    function showInputError($input, message, retryCallback) {
        try {
            const $parent = $input.parent();
            $parent.find('.chat-error-inline').remove();
            const $err = $(`
                <div class="chat-error-inline small text-danger mt-1 d-flex align-items-center">
                    <span class="flex-grow-1 me-2">${escapeHtml(message)}</span>
                    ${retryCallback ? '<button type="button" class="btn btn-sm btn-link chat-retry-btn">Retry</button>' : ''}
                </div>
            `);
            $parent.append($err);
            if (retryCallback) {
                $err.find('.chat-retry-btn').on('click', function () {
                    $err.remove();
                    try { retryCallback(); } catch (ex) { console.error('retry callback error', ex); }
                });
            }
            // auto-hide after a while
            setTimeout(() => $err.fadeOut(300, function () { $(this).remove(); }), 8000);
        } catch (ex) {
            console.error('showInputError error', ex);
        }
    }

    // Append a small system message to messages container
    function appendSystemMessage(message, type = 'danger') {
        try {
            const $container = $(MESSAGES_CONTAINER);
            if (!$container.length) {
                // fallback: log
                console.warn('Messages container not found:', MESSAGES_CONTAINER, 'Message:', message);
                return;
            }
            const safe = escapeHtml(message);
            const $msg = $(`<div class="chat-system-msg alert alert-${type} p-2 my-2 small">${safe}</div>`);
            $container.append($msg);
            $container.scrollTop($container[0].scrollHeight);
        } catch (ex) {
            console.error('appendSystemMessage error', ex);
        }
    }

    // Clear input and restore focus
    function clearInput($input) {
        try {
            // Prefer setting both value and property to ensure frameworks see the change
            if ($input && $input.length) {
                $input.each(function () {
                    try { this.value = ''; } catch (e) {}
                });
                $input.val('');
                $input.trigger('input');
                $input.trigger('change');
                $input.focus();
            }
        } catch (ex) {
            console.error('clearInput error', ex);
        }
    }

    // Escape HTML
    function escapeHtml(unsafe) {
        return String(unsafe)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/\'/g, '&#039;');
    }

    // Normalize sender function to return a Promise (evaluated per-call)
    function getSender() {
        return function (text, $input) {
            // If a consumer set a custom sender on window.chat, prefer it
            if (window.chat && typeof window.chat.sendMessage === 'function') {
                try {
                    const r = window.chat.sendMessage(text, $input);
                    return r && typeof r.then === 'function' ? r : Promise.resolve(r);
                } catch (ex) {
                    return Promise.reject(ex);
                }
            }

            // Try global window.sendMessage
            if (typeof window.sendMessage === 'function') {
                try {
                    const result = window.sendMessage(text);
                    return result && typeof result.then === 'function' ? result : Promise.resolve(result);
                } catch (ex) {
                    return Promise.reject(ex);
                }
            }

            // Fallback to AJAX form post
            const $form = $input.closest('form');
            if (!$form.length) return Promise.reject(new Error('No send function and no enclosing form'));
            const url = $form.attr('action') || window.location.href;
            return new Promise(function (resolve, reject) {
                $.ajax({
                    url: url,
                    method: ($form.attr('method') || 'POST').toUpperCase(),
                    data: $form.serialize(),
                    success: resolve,
                    error: function (xhr, status, err) { reject({ xhr, status, err }); }
                });
            });
        };
    }

    // Send with retries and exponential backoff; use dynamic sender per call
    function sendWithRetries(text, $input, attempt = 0) {
        return new Promise(function (resolve, reject) {
            try {
                const dynamicSender = getSender();
                dynamicSender(text, $input)
                    .then(res => resolve(res))
                    .catch(err => {
                        const shouldRetry = attempt < MAX_RETRIES;
                        if (shouldRetry) {
                            const delay = RETRY_BASE_MS * Math.pow(2, attempt);
                            console.warn(`send failed, retrying in ${delay}ms (attempt ${attempt + 1})`, err);
                            setTimeout(() => {
                                sendWithRetries(text, $input, attempt + 1).then(resolve).catch(reject);
                            }, delay);
                        } else {
                            reject(err);
                        }
                    });
            } catch (ex) {
                reject(ex);
            }
        });
    }

    // Prevent double sends and throttle
    function canSend($input) {
        try {
            if (!($input && $input.length)) return false;
            const el = $input.get(0);
            if (sendingMap.get(el)) return false;
            const last = lastSendTs.get(el) || 0;
            const now = Date.now();
            if (now - last < SEND_THROTTLE_MS) return false;
            lastSendTs.set(el, now);
            return true;
        } catch (ex) { console.error('canSend error', ex); return false; }
    }

    function setSending($input, isSending) {
        try { sendingMap.set($input.get(0), !!isSending); } catch (ex) { console.error('setSending error', ex); }
    }

    // Send provided text (used for optimistic clear behavior)
    function sendText(text, $input, $trigger) {
        try {
            if (!text || !text.trim()) return Promise.reject(new Error('empty'));
            if (!canSend($input)) return Promise.reject(new Error('throttled'));

            setSending($input, true);
            if ($trigger && $trigger.length) $trigger.prop('disabled', true);
            $input.prop('disabled', true);

            return sendWithRetries(text, $input)
                .then(res => {
                    notify('Message sent', 'info');
                    return res;
                })
                .catch(err => {
                    console.error('sendText failure', err);
                    return Promise.reject(err);
                })
                .finally(() => {
                    setSending($input, false);
                    try { $input.prop('disabled', false); } catch (e) {}
                    if ($trigger && $trigger.length) try { $trigger.prop('disabled', false); } catch (e) {}
                });
        } catch (ex) {
            setSending($input, false);
            return Promise.reject(ex);
        }
    }

    // Main send handler used by keyboard, button, and form (non-optimistic)
    function handleSend($input, $trigger) {
        try {
            const textRaw = ($input.val() || '');
            const text = textRaw.trim();
            if (!text) {
                showInputError($input, 'Cannot send empty message');
                return Promise.reject(new Error('empty'));
            }
            // optimistic: clear immediately and send in background; restore on failure
            const original = textRaw;
            clearInput($input);

            return sendText(text, $input, $trigger)
                .then(res => res)
                .catch(err => {
                    // restore original text so user doesn't lose message
                    try { $input.val(original); $input.trigger('input'); $input.focus(); } catch (ex) { console.error('restore input error', ex); }
                    const serverMsg = (err && err.xhr && err.xhr.responseText) ? err.xhr.responseText : (err && err.message) ? err.message : 'Send failed. Please try again.';
                    showInputError($input, serverMsg, function () { try { handleSend($input, $trigger); } catch (ex) { console.error('retry handleSend error', ex); } });
                    notify('Error sending message: ' + serverMsg, 'danger');
                    return Promise.reject(err);
                });
        } catch (ex) {
            console.error('handleSend error', ex);
            notify('Unexpected error while sending message', 'danger');
            return Promise.reject(ex);
        }
    }

    // Keyboard handler
    function attachInputHandler() {
        $(document).on('keydown', INPUT_SELECTOR, function (e) {
            try {
                const $this = $(this);
                if (e.key === 'Enter' && !e.shiftKey && !e.ctrlKey) {
                    e.preventDefault();
                    handleSend($this, null).catch(() => {});
                }
            } catch (ex) { console.error('input handler error', ex); }
        });

        // Click handler for send buttons
        $(document).on('click', SEND_BUTTON_SELECTOR, function (e) {
            try {
                e.preventDefault();
                const $btn = $(this);
                let $input = null;
                const target = $btn.data('target');
                if (target) $input = $(target);
                if ((!$input || !$input.length) && $btn.closest('form').length) $input = $btn.closest('form').find(INPUT_SELECTOR).first();
                if ((!$input || !$input.length)) $input = $(INPUT_SELECTOR).first();
                if ((!$input || !$input.length)) $input = $('#floating-chat-input');
                if ((!$input || !$input.length)) { notify('Chat input not found', 'danger'); return; }

                const textRaw = ($input.val() || '');
                const text = textRaw.trim();
                if (!text) { showInputError($input, 'Cannot send empty message'); return; }

                // Optimistic clear: remove text immediately and send in background. Restore on failure.
                const original = textRaw;
                clearInput($input);

                sendText(text, $input, $btn)
                    .catch(err => {
                        // restore original text
                        try { $input.val(original); $input.trigger('input'); $input.focus(); } catch (ex) { console.error('restore input error', ex); }
                        const serverMsg = (err && err.xhr && err.xhr.responseText) ? err.xhr.responseText : (err && err.message) ? err.message : 'Send failed. Please try again.';
                        showInputError($input, serverMsg, function () { try { handleSend($input, $btn); } catch (ex) { console.error('retry handleSend error', ex); } });
                        notify('Error sending message: ' + serverMsg, 'danger');
                    });

            } catch (ex) { console.error('send button handler error', ex); }
        });

        // Dedicated handler for common floating-send widgets that target the specific input
        $(document).on('click', FLOATING_SEND_SELECTORS, function (e) {
            try {
                e.preventDefault();
                const $btn = $(this);
                const $input = $('#floating-chat-input');
                if (!$input || !$input.length) { notify('Chat input not found', 'danger'); return; }

                const original = ($input.val() || '');
                const text = original.trim();
                if (!text) { showInputError($input, 'Cannot send empty message'); return; }

                // optimistic clear for instant UX
                clearInput($input);

                sendText(text, $input, $btn)
                    .catch(err => {
                        // restore original text
                        try { $input.val(original); $input.trigger('input'); $input.focus(); } catch (ex) { console.error('restore input error', ex); }
                        const serverMsg = (err && err.xhr && err.xhr.responseText) ? err.xhr.responseText : (err && err.message) ? err.message : 'Send failed. Please try again.';
                        showInputError($input, serverMsg, function () { try { handleSend($input, $btn); } catch (ex) { console.error('retry handleSend error', ex); } });
                        notify('Error sending message: ' + serverMsg, 'danger');
                    });
            } catch (ex) { console.error('floating send handler error', ex); }
        });

        // Global delegated handler to catch send clicks that weren't matched by specific selectors
        $(document).on('click', function (e) {
            try {
                const $targetBtn = $(e.target).closest('button, a, [role="button"], .btn');
                if (!$targetBtn.length) return; // not a clickable control

                // Avoid double-handling buttons that our other handlers already match
                if ($targetBtn.is(SEND_BUTTON_SELECTOR) || $targetBtn.is(FLOATING_SEND_SELECTORS)) return;

                // Try to find the floating input within related containers
                const $input = $targetBtn.closest('form, .chat, .floating-chat, .input-group, .chat-container').find('#floating-chat-input').first() || $('#floating-chat-input');
                if (!$input || !$input.length) return; // no floating input in scope

                const original = ($input.val() || '');
                const text = original.trim();
                if (!text) return; // nothing to send

                // Prevent native navigation/submit so we can handle send optimistically
                e.preventDefault();

                // Clear immediately for UX
                clearInput($input);

                // Send in background; restore input on failure
                sendText(text, $input, $targetBtn)
                    .catch(err => {
                        try { $input.val(original); $input.trigger('input'); $input.focus(); } catch (ex) { console.error('restore input error', ex); }
                        const serverMsg = (err && err.xhr && err.xhr.responseText) ? err.xhr.responseText : (err && err.message) ? err.message : 'Send failed. Please try again.';
                        showInputError($input, serverMsg, function () { try { handleSend($input, $targetBtn); } catch (ex) { console.error('retry handleSend error', ex); } });
                        notify('Error sending message: ' + serverMsg, 'danger');
                    });

            } catch (ex) { console.error('global delegated send handler error', ex); }
        });

        // Intercept chat-form submits
        $(document).on('submit', 'form.chat-form', function (e) {
            try {
                e.preventDefault();
                const $form = $(this);
                const $input = $form.find(INPUT_SELECTOR).first();
                if (!$input || !$input.length) { notify('Chat input not found', 'danger'); return; }
                const $submit = $form.find(SEND_BUTTON_SELECTOR).first();
                handleSend($input, $submit).catch(() => {});
            } catch (ex) { console.error('chat-form submit error', ex); }
        });
    }

    // Incoming messages handler (exposed)
    function handleIncomingMessageSafe(data) {
        try {
            // allow custom handler
            if (typeof window.onChatMessage === 'function') {
                try { window.onChatMessage(data); return; } catch (ex) { console.error('user onChatMessage failed', ex); }
            }
            const $container = $(MESSAGES_CONTAINER);
            if (!$container.length) { console.warn('No messages container', MESSAGES_CONTAINER); return; }
            let text = '';
            if (data == null) text = '';
            else if (typeof data === 'string') text = data;
            else if (data.message) text = data.message;
            else text = JSON.stringify(data);
            const $messageNode = $(`<div class="chat-msg incoming p-2 mb-2"><div class="small text-muted">${escapeHtml(text)}</div></div>`);
            $container.append($messageNode);
            $container.scrollTop($container[0].scrollHeight);
        } catch (ex) { console.error('handleIncomingMessageSafe error', ex); notify('Error receiving message', 'danger'); }
    }

    // expose
    window.chat = window.chat || {};
    window.chat.handleIncomingMessage = handleIncomingMessageSafe;
    window.chat.appendSystemMessage = appendSystemMessage;
    window.chat.setSendOverride = function (fn) {
        if (typeof fn === 'function') {
            window.chat.sendMessage = fn;
        }
    };

    // init
    $(function () { attachInputHandler(); });

})(jQuery);
