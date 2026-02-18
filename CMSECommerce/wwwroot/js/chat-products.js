var ChatProductsModule = (function () {
    return {
        init: function (config) {
            // Destructure config and default chatEnabled to true if not provided
            const { cS, cST, cSO, pIU, currentUserId, chatEnabled = true } = config;

            function buildUrl(b, s, t, o, p) {
                let u = b, q = [];
                if (s && s.length > 0) u = u.replace(/\/$/, '') + "/" + encodeURIComponent(s);
                if (t && t.length > 0) q.push("searchTerm=" + encodeURIComponent(t));
                if (o && o !== "default") q.push("sortOrder=" + encodeURIComponent(o));
                if (p && p > 1) q.push("p=" + p);
                if (q.length > 0) u += (u.includes('?') ? '&' : '?') + q.join("&");
                return u;
            }

            document.addEventListener("DOMContentLoaded", function () {
                // --- PRODUCT FUNCTIONALITY (Always Active) ---
                var sD = document.getElementById("sortDropdown");
                if (sD && cSO) sD.value = cSO;
                if (sD) {
                    sD.addEventListener("change", function () {
                        window.location.href = buildUrl(pIU, cS, cST, sD.value, 1)
                    });
                }

                // --- CHAT & SIGNALR FUNCTIONALITY (Conditional) ---
                if (!chatEnabled) {
                    console.log("Chat system disabled via config.");
                    return; // Exit early to prevent SignalR connection and event binding
                }

                const cWC = document.getElementById('floating-chat-modal') || document.getElementById('chat-window-container'),
                    cM = document.getElementById('floating-chat-messages') || document.getElementById('chat-messages'),
                    cI = document.getElementById('floating-chat-input') || document.getElementById('chat-input'),
                    sB = document.getElementById('floating-chat-send') || document.getElementById('send-btn'),
                    cCB = document.getElementById('floating-chat-close') || document.getElementById('close-chat-btn'),
                    gCL = document.getElementById('group-chat-link'),
                    cTN = document.getElementById('floating-chat-title') || document.getElementById('chat-target-name'),
                    uLV = document.getElementById('user-list-view');

                let gC = document.getElementById('group-controls');
                if (!gC && cWC) {
                    gC = document.createElement('div');
                    gC.id = 'group-controls';
                    gC.className = 'mt-2';
                    if (cM && cWC.contains(cM)) cWC.insertBefore(gC, cM)
                }

                let cTID = null, iGC = false, cGN = null;
                const conn = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();

                conn.start().catch(e => console.error(e.toString()));

                conn.on("ReceiveMessage", (s, m) => {
                    const e = `<div class="chat-message-item mb-1 small"><span class="fw-bold">${s}:</span> ${m}</div>`;
                    if (cM) { cM.insertAdjacentHTML('beforeend', e); cM.scrollTop = cM.scrollHeight }
                });

                conn.on("LoadHistory", ms => {
                    if (!cM) return; cM.innerHTML = ''; ms.forEach(m => {
                        const s = m.SenderName || m.senderName || m.sender || 'Unknown',
                            c = m.MessageContent || m.messageContent || m.message || '',
                            r = m.Timestamp || m.timestamp || m.Time || m.time || null;
                        let tW = ''; if (r) { const d = new Date(r); if (!isNaN(d.getTime())) tW = d.toLocaleString() }
                        const tH = tW ? `<div class="small text-muted">${tW}</div>` : '';
                        cM.insertAdjacentHTML('beforeend', `<div class="chat-message-item mb-1 small"><div><span class="fw-bold">${s}:</span> ${c}</div>${tH}</div>`)
                    }); cM.scrollTop = cM.scrollHeight
                });

                conn.on("ReceivePrivateMessage", (sID, sN, m) => {
                    if (currentUserId && currentUserId === sID) return;
                    document.querySelectorAll('[data-user-id="' + sID + '"]').forEach(el => {
                        const li = el.closest('li'); if (!li) return;
                        let b = li.querySelector('.unread-badge');
                        if (!b) { b = document.createElement('span'); b.className = 'badge bg-danger ms-2 unread-badge'; b.textContent = '1'; li.querySelector('a').appendChild(b) }
                        else b.textContent = (parseInt(b.textContent || '0') + 1).toString()
                    });
                    if (cTID === sID && !iGC) {
                        if (cM) { cM.insertAdjacentHTML('beforeend', `<div class="chat-message-item mb-1 small"><span class="fw-bold">${sN}:</span> ${m}</div>`); cM.scrollTop = cM.scrollHeight }
                        try { conn.invoke('MarkMessagesRead', sID) } catch (e) { }
                    } else {
                        openChatWindow(sID, sN, false);
                        if (cM) { cM.insertAdjacentHTML('beforeend', `<div class="chat-message-item mb-1 small"><span class="fw-bold">${sN}:</span> ${m}</div>`); cM.scrollTop = cM.scrollHeight }
                    }
                });

                function openChatWindow(tID, tN, isG) {
                    cTID = tID; iGC = isG; cGN = isG ? tID : null;
                    if (cTN) cTN.textContent = isG ? `Group: ${tN}` : `Chatting with: ${tN}`;
                    if (cM) cM.innerHTML = '';
                    const fM = document.getElementById('floating-chat-modal'), fUL = document.getElementById('floating-user-list'), uT = document.getElementById('floating-chat-users-toggle');
                    if (fUL) fUL.style.display = 'none'; if (cM) cM.style.display = 'block';
                    if (uT) uT.setAttribute('aria-pressed', 'false'); if (fM) fM.style.display = 'block';
                    else if (cWC) { cWC.style.display = 'block'; if (uLV) uLV.style.display = 'none' }
                    if (cI) cI.focus();
                    try { conn.invoke('GetRecentMessages', tID, isG).catch(e => console.error(e)) } catch (e) { }
                    if (!isG) {
                        document.querySelectorAll('[data-user-id="' + tID + '"]').forEach(el => { const b = el.querySelector('.unread-badge'); if (b) b.remove() });
                        try { conn.invoke('MarkMessagesRead', tID) } catch (e) { }
                    }
                }

                function closeChatWindow() {
                    cTID = null; iGC = false; cGN = null;
                    const fM = document.getElementById('floating-chat-modal');
                    if (fM) fM.style.display = 'none';
                    else if (cWC) { cWC.style.display = 'none'; if (uLV) uLV.style.display = 'block' }
                    if (cI) cI.value = ''; if (gC) gC.innerHTML = ''
                }

                document.addEventListener('click', e => {
                    const b = e.target.closest && e.target.closest('.start-chat-btn');
                    if (!b) return; e.preventDefault(); openChatWindow(b.getAttribute('data-user-id'), b.getAttribute('data-user-name'), false)
                });

                document.addEventListener('keydown', e => {
                    if (e.key === 'Enter' || e.key === ' ') {
                        const a = document.activeElement; if (!a) return;
                        if (a.classList && a.classList.contains('start-chat-btn')) { e.preventDefault(); openChatWindow(a.getAttribute('data-user-id'), a.getAttribute('data-user-name'), false) }
                        if (a.id === 'group-chat-link') { e.preventDefault(); conn.invoke('JoinGroup', 'Global'); openChatWindow('Global', 'Global', true) }
                    }
                });

                if (gCL) gCL.addEventListener('click', e => { e.preventDefault(); conn.invoke('JoinGroup', 'Global'); openChatWindow('Global', 'Global', true) });

                var oCB = document.getElementById('open-chat-toggle');
                if (oCB) {
                    oCB.setAttribute('role', 'button');
                    oCB.addEventListener('click', e => {
                        e.preventDefault(); if (gCL) { try { gCL.click(); return } catch (err) { } }
                        conn.invoke('JoinGroup', 'Global'); openChatWindow('Global', 'Global', true)
                    });
                    oCB.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); oCB.click() } });
                }

                const fCl = document.getElementById('floating-chat-close'), fMi = document.getElementById('floating-chat-minimize');
                if (fCl) fCl.addEventListener('click', closeChatWindow);
                if (fMi) fMi.addEventListener('click', () => { const m = document.getElementById('floating-chat-modal'); if (m) m.style.display = 'none' });
                if (cCB && !document.getElementById('floating-chat-modal')) cCB.addEventListener('click', closeChatWindow);

                const aSB = document.getElementById('floating-chat-send') || sB, aCI = document.getElementById('floating-chat-input') || cI;
                if (aSB) aSB.addEventListener('click', sendMessage);
                if (aCI) aCI.addEventListener('keypress', e => { if (e.key === 'Enter') sendMessage() });

                function sendMessage() {
                    const i = document.getElementById('floating-chat-input') || cI, m = i.value.trim();
                    if (m.length === 0 || conn.state !== "Connected") return;
                    if (iGC && cGN) conn.invoke("SendMessageToGroup", cGN, m).catch(e => console.error(e));
                    else if (cTID) {
                        conn.invoke("SendPrivateMessage", cTID, m).catch(e => console.error(e));
                        const sM = `<div class="chat-message-item self-message mb-1 small text-end"><span class="fw-bold text-amazon-teal">You:</span> ${m}</div>`;
                        if (cM) { cM.insertAdjacentHTML('beforeend', sM); cM.scrollTop = cM.scrollHeight }
                    }
                    i.value = ''
                }

                const uT = document.getElementById('floating-chat-users-toggle'), fUL = document.getElementById('floating-user-list');
                if (uT && fUL) {
                    uT.addEventListener('click', e => {
                        e.preventDefault(); const p = uT.getAttribute('aria-pressed') === 'true';
                        if (p) { fUL.style.display = 'none'; if (document.getElementById('floating-chat-messages')) document.getElementById('floating-chat-messages').style.display = 'block'; uT.setAttribute('aria-pressed', 'false') }
                        else {
                            const sV = document.getElementById('user-list-view');
                            fUL.innerHTML = sV ? sV.innerHTML : '<div class="small text-muted">No users available.</div>';
                            fUL.style.display = 'block'; if (document.getElementById('floating-chat-messages')) document.getElementById('floating-chat-messages').style.display = 'none';
                            uT.setAttribute('aria-pressed', 'true')
                        }
                    });
                }
            });
        }
    };
})();