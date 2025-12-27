"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

let typingTimeout;

connection.start().catch(err => console.error(err.toString()));

// --- 1. SIGNALR LISTENERS ---

connection.on("UserStatusChanged", (userId, isOnline) => {
    const statusDot = document.getElementById(`status-${userId}`);
    if (statusDot) {
        statusDot.classList.toggle("online", isOnline);
        statusDot.classList.toggle("offline", !isOnline);
    }
    const activeId = document.getElementById("activeUserId").value;
    if (activeId === userId) updateHeaderStatus(isOnline);
});

connection.on("UserTyping", (userId, isTyping) => {
    const indicator = document.getElementById(`typing-${userId}`);
    if (indicator) {
        isTyping ? indicator.classList.remove("d-none") : indicator.classList.add("d-none");
    }
});

connection.on("MessagesRead", (userId) => {
    const activeId = document.getElementById("activeUserId").value;
    if (activeId === userId) {
        document.querySelectorAll('.msg-sent .read-status-icon').forEach(el => {
            el.innerHTML = '<i class="bi bi-check2-all text-info"></i>';
        });
    }
});

connection.on("MessageDeleted", (messageId) => {
    const el = document.getElementById(`msg-${messageId}`);
    if (el) {
        el.style.opacity = "0";
        setTimeout(() => el.remove(), 300);
    }
});

connection.on("ReceiveMessage", (senderId, message, timestamp, isFile = false, fileName = "") => {
    const activeId = document.getElementById("activeUserId").value;

    if (activeId === senderId) {
        appendMessage(message, "received", timestamp, false, 0, isFile, fileName);
        scrollToBottom(false);
        connection.invoke("MarkAsRead", senderId);
        document.getElementById(`typing-${senderId}`).classList.add("d-none");
    } else {
        const badge = document.getElementById(`badge-${senderId}`);
        if (badge) {
            let count = parseInt(badge.innerText) || 0;
            badge.innerText = count + 1;
            badge.classList.remove("d-none");
        }
    }
    const preview = document.getElementById(`last-msg-${senderId}`);
    if (preview) preview.innerText = isFile ? "Sent a file" : message;
});

// --- 2. CORE ACTIONS ---

async function selectContact(userId, name) {
    document.getElementById("activeUserId").value = userId;
    document.getElementById("chatWithTitle").innerText = name;

    document.getElementById("messageInput").disabled = false;
    document.getElementById("sendButton").disabled = false;
    document.getElementById("attachButton").disabled = false;

    document.querySelectorAll('.contact-item').forEach(i => i.classList.remove('active'));
    const contactEl = document.getElementById(`contact-${userId}`);
    if (contactEl) {
        contactEl.classList.add('active');
        const isOnline = contactEl.querySelector('.status-dot').classList.contains('online');
        updateHeaderStatus(isOnline);
    }

    await connection.invoke("MarkAsRead", userId);
    const badge = document.getElementById(`badge-${userId}`);
    if (badge) { badge.innerText = "0"; badge.classList.add('d-none'); }

    const list = document.getElementById("messagesList");
    list.innerHTML = `<div class="text-center text-muted my-auto">Loading...</div>`;

    const res = await fetch(`/Chat/GetChatHistory/${userId}`);
    const data = await res.json();
    list.innerHTML = "";
    data.forEach(m => appendMessage(m.content, m.type, m.timestamp, m.isRead, m.id, m.isFile, m.fileName));
    scrollToBottom(true);
}

// UPDATED: Handle File Upload with Loading Spinner UI
async function uploadAndSendFile() {
    const fileInput = document.getElementById('fileInput');
    const attachBtn = document.getElementById('attachButton');
    const attachIcon = document.getElementById('attachIcon');
    const spinner = document.getElementById('uploadSpinner');
    const receiverId = document.getElementById("activeUserId").value;

    if (fileInput.files.length === 0 || !receiverId) return;

    // UI: Show loading state
    attachBtn.disabled = true;
    if (attachIcon) attachIcon.classList.add('d-none');
    if (spinner) spinner.classList.remove('d-none');

    const file = fileInput.files[0];
    const formData = new FormData();
    formData.append("file", file);

    try {
        // 1. Upload to server
        const response = await fetch('/Chat/UploadFile', { method: 'POST', body: formData });
        const data = await response.json();

        // 2. Determine if it's an image
        const isImage = /\.(jpg|jpeg|png|webp|gif)$/i.test(data.name);

        // 3. Send via SignalR
        await connection.invoke("SendFileMessage", receiverId, data.url, data.name, isImage);

        // 4. Reload local state
        await selectContact(receiverId, document.getElementById("chatWithTitle").innerText);

        fileInput.value = "";
    } catch (err) {
        console.error("Upload failed:", err);
        alert("Failed to upload file.");
    } finally {
        // UI: Reset state
        attachBtn.disabled = false;
        if (attachIcon) attachIcon.classList.remove('d-none');
        if (spinner) spinner.classList.add('d-none');
    }
}

async function sendMessage() {
    const input = document.getElementById("messageInput");
    const id = document.getElementById("activeUserId").value;
    const msg = input.value.trim();

    if (!msg) return;

    connection.invoke("SendTypingNotification", id, false);
    await connection.invoke("SendPrivateMessage", id, msg);
    await selectContact(id, document.getElementById("chatWithTitle").innerText);

    input.value = "";
    scrollToBottom(true);
}

async function deleteMsg(id) {
    if (confirm("Delete this message?")) {
        await connection.invoke("DeleteMessage", id);
    }
}

// --- 3. UI HELPERS ---

function updateHeaderStatus(isOnline) {
    const header = document.getElementById("activeUserStatus");
    if (!header) return;
    header.classList.remove("d-none");
    const dot = header.querySelector(".status-dot");
    dot.className = `status-dot ${isOnline ? 'online' : 'offline'} me-1`;
    header.lastChild.textContent = isOnline ? " Active Now" : " Offline";
}

function handleKeyUp(e) {
    if (e.key === "Enter") { sendMessage(); return; }
    const rid = document.getElementById("activeUserId").value;
    if (!rid) return;

    connection.invoke("SendTypingNotification", rid, true);
    clearTimeout(typingTimeout);
    typingTimeout = setTimeout(() => {
        connection.invoke("SendTypingNotification", rid, false);
    }, 2000);
}

function appendMessage(content, type, time, isRead, id, isFile = false, fileName = "") {
    const formattedTime = new Date(time).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    const isSent = type === 'sent';

    let bodyContent = `<span>${content}</span>`;

    if (isFile) {
        const isImage = /\.(jpg|jpeg|png|webp|gif)$/i.test(fileName);
        if (isImage) {
            bodyContent = `<img src="${content}" class="chat-image" onclick="window.open('${content}')" title="${fileName}">`;
        } else {
            bodyContent = `
                <a href="${content}" target="_blank" class="file-attachment">
                    <i class="bi bi-file-earmark-arrow-down fs-4 me-2"></i>
                    <span class="text-truncate" style="max-width: 150px;">${fileName}</span>
                </a>`;
        }
    }

    const html = `
        <div class="msg-bubble ${isSent ? 'msg-sent' : 'msg-received'}" id="msg-${id}">
            <div class="d-flex justify-content-between align-items-start">
                ${bodyContent}
                ${isSent && id !== 0 ? `<i class="bi bi-trash3 text-light ms-2" style="cursor:pointer; font-size: 0.75rem;" onclick="deleteMsg(${id})"></i>` : ''}
            </div>
            <div class="d-flex justify-content-end align-items-center mt-1" style="gap: 5px;">
                <small style="font-size: 0.6rem; opacity: 0.8;">${formattedTime}</small>
                ${isSent ? `<span class="read-status-icon">${isRead ? '<i class="bi bi-check2-all text-info"></i>' : '<i class="bi bi-check2"></i>'}</span>` : ''}
            </div>
        </div>`;
    document.getElementById("messagesList").insertAdjacentHTML('beforeend', html);
}

function scrollToBottom(force = false) {
    const el = document.getElementById("messagesList");
    if (force || (el.scrollHeight - el.clientHeight <= el.scrollTop + 150)) {
        el.scrollTop = el.scrollHeight;
    }
}