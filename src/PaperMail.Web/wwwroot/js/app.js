// Minimal vanilla JS for E-Ink optimization
// Total size target: <3KB

(function() {
  'use strict';

  // Keyboard shortcuts for navigation
  const KeyboardNav = {
    init() {
      document.addEventListener('keydown', this.handleKeyPress.bind(this));
    },

    handleKeyPress(e) {
      // Only on inbox and email detail pages
      if (!document.body.classList.contains('keyboard-nav-enabled')) return;

      // Don't trigger shortcuts when typing in inputs
      if (e.target.matches('input, textarea, select')) return;

      switch(e.key) {
        case 'j': // Next email
          this.navigateToNext();
          break;
        case 'k': // Previous email
          this.navigateToPrev();
          break;
        case 'r': // Reply
          this.reply();
          break;
        case 'c': // Compose
          window.location.href = '/compose';
          break;
        case 'i': // Inbox
          window.location.href = '/inbox';
          break;
        case '/': // Search (future)
          e.preventDefault();
          this.focusSearch();
          break;
        case '?': // Show keyboard shortcuts help
          e.preventDefault();
          this.showHelp();
          break;
      }
    },

    navigateToNext() {
      const emails = document.querySelectorAll('.email-item a');
      const currentUrl = window.location.pathname;
      
      for (let i = 0; i < emails.length; i++) {
        if (emails[i].getAttribute('href') === currentUrl && emails[i + 1]) {
          window.location.href = emails[i + 1].getAttribute('href');
          break;
        }
      }
    },

    navigateToPrev() {
      const emails = document.querySelectorAll('.email-item a');
      const currentUrl = window.location.pathname;
      
      for (let i = 0; i < emails.length; i++) {
        if (emails[i].getAttribute('href') === currentUrl && emails[i - 1]) {
          window.location.href = emails[i - 1].getAttribute('href');
          break;
        }
      }
    },

    reply() {
      const replyBtn = document.querySelector('a[href*="reply"]');
      if (replyBtn) window.location.href = replyBtn.getAttribute('href');
    },

    focusSearch() {
      const searchInput = document.querySelector('input[type="search"]');
      if (searchInput) searchInput.focus();
    },

    showHelp() {
      const helpText = `
Keyboard Shortcuts:
  j - Next email
  k - Previous email
  r - Reply
  c - Compose new email
  i - Go to inbox
  / - Search
  ? - Show this help
      `.trim();
      
      alert(helpText);
    }
  };

  // Form validation to prevent unnecessary server roundtrips
  const FormValidation = {
    init() {
      const forms = document.querySelectorAll('form[data-validate]');
      forms.forEach(form => {
        form.addEventListener('submit', this.validateForm.bind(this));
      });
    },

    validateForm(e) {
      const form = e.target;
      const errors = [];

      // Email validation
      const emailInputs = form.querySelectorAll('input[type="email"]');
      emailInputs.forEach(input => {
        if (input.value && !this.isValidEmail(input.value)) {
          errors.push(`Invalid email: ${input.value}`);
        }
      });

      // Required field validation
      const requiredInputs = form.querySelectorAll('[required]');
      requiredInputs.forEach(input => {
        if (!input.value.trim()) {
          errors.push(`${input.name || 'Field'} is required`);
        }
      });

      if (errors.length > 0) {
        e.preventDefault();
        alert('Please fix the following errors:\n\n' + errors.join('\n'));
        return false;
      }

      return true;
    },

    isValidEmail(email) {
      return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }
  };

  // LocalStorage for read state caching (optional optimization)
  const ReadStateCache = {
    init() {
      // Mark emails as read in localStorage to show immediately
      const markReadButtons = document.querySelectorAll('[data-mark-read]');
      markReadButtons.forEach(btn => {
        btn.addEventListener('click', this.markAsRead.bind(this));
      });

      // Apply cached read states on inbox load
      this.applyCachedStates();
    },

    markAsRead(e) {
      const emailId = e.target.dataset.markRead;
      if (emailId) {
        const readEmails = this.getReadEmails();
        readEmails.add(emailId);
        localStorage.setItem('readEmails', JSON.stringify([...readEmails]));
      }
    },

    getReadEmails() {
      try {
        const stored = localStorage.getItem('readEmails');
        return new Set(stored ? JSON.parse(stored) : []);
      } catch {
        return new Set();
      }
    },

    applyCachedStates() {
      const readEmails = this.getReadEmails();
      readEmails.forEach(emailId => {
        const emailItem = document.querySelector(`[data-email-id="${emailId}"]`);
        if (emailItem) {
          emailItem.classList.remove('unread');
          emailItem.classList.add('read');
        }
      });
    }
  };

  // Initialize on DOM ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  function init() {
    KeyboardNav.init();
    FormValidation.init();
    ReadStateCache.init();

    // Add keyboard nav class to relevant pages
    if (window.location.pathname.includes('/inbox') || 
        window.location.pathname.includes('/email/')) {
      document.body.classList.add('keyboard-nav-enabled');
    }

    // Log that JS is loaded (helpful for debugging E-Ink devices)
    console.log('PaperMail initialized');
  }
})();
