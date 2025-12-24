(function($){
  'use strict';

  $(function(){
    // ensure markup present
    if (!$('#chat-widget').length) return;

    // toggle minimize
    function setMinimized(min){
      if(min) $('#chat-widget').addClass('minimized');
      else $('#chat-widget').removeClass('minimized');
    }

    // header click toggles
    $('#chat-widget .chat-header').on('click keypress', function(e){
      if(e.type==='click' || (e.type==='keypress' && (e.key==='Enter' || e.key===' '))){
        const minimized = $('#chat-widget').hasClass('minimized');
        const newMinimized = !minimized;
        setMinimized(newMinimized);

        // Load contacts when opening chat
        if (!newMinimized && window.chat && window.chat.connection && window.chat.connection.state === signalR.HubConnectionState.Connected) {
          loadOrderContacts();
        }
      }
    });

    // close
    $('#chat-widget .chat-close').on('click', function(e){
      e.stopPropagation();
      $('#chat-widget').remove();
    });

    // contact switching
    $('#chat-widget').on('click', '.contact-item', function(){
      $(this).siblings().removeClass('active');
      $(this).addClass('active');
      $('#chatMessages').empty();
      appendSystemMessage('Conversation started with ' + $(this).find('.contact-name').text(),'info');
    });

    // append helper
    function appendMessage(text, who){
      const cls = who==='me'? 'chat-msg me' : 'chat-msg';
      const node = $('<div>').addClass(cls).text(text);
      $('#chatMessages').append(node);
      $('#chatMessages').scrollTop($('#chatMessages')[0].scrollHeight);
    }

    function appendSystemMessage(text, type){
      const node = $('<div>').addClass('chat-msg system small text-muted').text(text);
      $('#chatMessages').append(node);
      $('#chatMessages').scrollTop($('#chatMessages')[0].scrollHeight);
    }

    // wire send from our global chat.js API if present
    window.chat = window.chat || {};
    // if someone set setSendOverride earlier, leave it; otherwise create a default that echoes
    if(!window.chat.sendMessage){
      window.chat.sendMessage = function(text, $input){
        return new Promise(function(resolve){
          // echo locally and simulate server response
          appendMessage(text,'me');
          setTimeout(function(){ appendMessage('Auto-reply: ' + text, 'support'); resolve(); }, 600);
        });
      };
    }

    // forward the UI send to window.chat.sendMessage
    $('#floating-chat-send').on('click', function(){
      const $input = $('#floating-chat-input');
      const text = ($input.val()||'').trim();
      if(!text) return;
      // optimistic clear
      const original = $input.val();
      $input.val(''); $input.trigger('input');

      // send
      Promise.resolve(window.chat.sendMessage(text,$input)).then(function(){
        appendMessage(text,'me');
      }).catch(function(err){
        // restore on failure
        $input.val(original); $input.trigger('input');
        appendSystemMessage('Failed to send message','danger');
      });
    });

    // enter key
    $('#floating-chat-input').on('keydown', function(e){
      if(e.key==='Enter' && !e.shiftKey){ e.preventDefault(); $('#floating-chat-send').click(); }
    });

    // Load order contacts function
    function loadOrderContacts() {
      if (window.chat && window.chat.connection && window.chat.connection.state === signalR.HubConnectionState.Connected) {
        $('.contacts-loading').show();
        $('.contacts-list').hide();
        $('#contactsContainer').empty();
        window.chat.connection.invoke('GetOrderContacts').catch(function (err) {
          console.error('Error loading contacts:', err);
          $('.contacts-loading').html('<small>Error loading contacts</small>');
        });
      }
    }

    // Handler for LoadContacts from server
    window.chat.handleLoadContacts = function(contacts) {
      $('.contacts-loading').hide();
      $('.contacts-list').show();
      $('#contactsContainer').empty();

      if (!contacts || contacts.length === 0) {
        $('#contactsContainer').append('<div class="text-muted small p-2">No order contacts found</div>');
        return;
      }

      contacts.forEach(function(contact) {
        const statusClass = contact.IsOnline ? 'online' : 'offline';
        const statusText = contact.IsOnline ? 'Online' : 'Offline';
        const contactHtml = `
          <div class="contact-item d-flex align-items-center p-2 border-bottom" data-userid="${contact.UserId}" style="cursor: pointer;">
            <div class="contact-status ${statusClass} me-2" style="width: 8px; height: 8px; border-radius: 50%; background-color: ${contact.IsOnline ? 'green' : 'gray'};"></div>
            <div class="contact-name flex-grow-1">${escapeHtml(contact.Name)}</div>
            <small class="text-muted">${statusText}</small>
          </div>
        `;
        $('#contactsContainer').append(contactHtml);
      });

      // Click handler for contacts
      $('#contactsContainer').on('click', '.contact-item', function() {
        const userId = $(this).data('userid');
        const userName = $(this).find('.contact-name').text();

        // Set active contact
        $('.contact-item').removeClass('active');
        $(this).addClass('active');

        // Clear messages and load history
        $('#chatMessages').empty();
        appendSystemMessage(`Starting conversation with ${userName}`, 'info');

        if (window.chat && window.chat.connection) {
          window.chat.connection.invoke('GetRecentMessages', userId, false).catch(function(err) {
            console.error('Error loading message history:', err);
          });
        }

        // Store current chat user for sending messages
        window.currentChatUserId = userId;
      });
    };

    // Expose loadOrderContacts globally
    window.loadOrderContacts = loadOrderContacts;

  });
})(jQuery);
