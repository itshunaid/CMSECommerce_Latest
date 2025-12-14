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
        setMinimized(!minimized);
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

  });
})(jQuery);