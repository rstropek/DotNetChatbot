import './index.css';
import { marked } from 'marked';

class ChatBot {
  constructor() {
    this.conversationId = null;
    this.conversationHistory = document.getElementById('conversation-history');
    this.messageInput = document.getElementById('message-input');
    this.submitButton = document.getElementById('submit-button');
    this.chatForm = document.getElementById('chat-form');
    this.implSelect = document.getElementById('impl-select');
    this.newConversationBtn = document.getElementById('new-conversation-btn');

    // URL prefix changes based on selected implementation
    this.apiPrefix = '/conversations';

    this.init();
  }

  get apiPrefix() {
    return this._apiPrefix;
  }

  set apiPrefix(value) {
    this._apiPrefix = value;
  }

  init() {
    this.chatForm.addEventListener('submit', (e) => this.handleSubmit(e));

    // When switching implementation, reset conversation
    this.implSelect.addEventListener('change', () => {
      this.apiPrefix = this.implSelect.value === 'agentframework'
        ? '/af/conversations'
        : '/conversations';
      sessionStorage.removeItem('conversationId');
      sessionStorage.removeItem('implementation');
      this.resetConversation();
    });

    // New Conversation button
    this.newConversationBtn.addEventListener('click', () => {
      sessionStorage.removeItem('conversationId');
      this.resetConversation();
    });

    // Restore implementation from sessionStorage
    const savedImpl = sessionStorage.getItem('implementation');
    if (savedImpl) {
      this.implSelect.value = savedImpl;
      this.apiPrefix = savedImpl === 'agentframework'
        ? '/af/conversations'
        : '/conversations';
    }

    // Try to restore existing conversation or create a new one
    const savedConversationId = sessionStorage.getItem('conversationId');
    if (savedConversationId) {
      this.conversationId = parseInt(savedConversationId, 10);
      this.loadHistory();
    } else {
      this.createConversation();
    }
  }

  resetConversation() {
    // Clear chat history in UI
    this.conversationHistory.innerHTML = '';
    const welcomeDiv = document.createElement('div');
    welcomeDiv.className = 'welcome-message';
    welcomeDiv.innerHTML = '<p>Welcome to the Chat Bot! Start a conversation below.</p>';
    this.conversationHistory.appendChild(welcomeDiv);

    // Create a new conversation with the current implementation
    this.conversationId = null;
    this.createConversation();
  }

  async createConversation() {
    try {
      const response = await fetch(`${__SERVICE_BASE__}${this.apiPrefix}/`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      this.conversationId = data.conversationId;

      // Persist to sessionStorage
      sessionStorage.setItem('conversationId', this.conversationId);
      sessionStorage.setItem('implementation', this.implSelect.value);

      console.log('Conversation created with ID:', this.conversationId, '(impl:', this.implSelect.value, ')');
    } catch (error) {
      console.error('Failed to create conversation:', error);
      this.addMessage('Sorry, I couldn\'t connect to the chat service. Please try refreshing the page.', 'assistant');
    }
  }

  async loadHistory() {
    try {
      const response = await fetch(`${__SERVICE_BASE__}${this.apiPrefix}/${this.conversationId}`);

      if (!response.ok) {
        // Conversation not found — create a new one
        sessionStorage.removeItem('conversationId');
        this.createConversation();
        return;
      }

      const messages = await response.json();

      if (messages.length === 0) {
        return;
      }

      // Clear welcome message and render history
      this.conversationHistory.innerHTML = '';
      for (const msg of messages) {
        this.addMessage(msg.content, msg.role === 'user' ? 'user' : 'assistant');
      }

      console.log('Loaded', messages.length, 'messages for conversation', this.conversationId);
    } catch (error) {
      console.error('Failed to load history:', error);
      // Fall back to creating a new conversation
      sessionStorage.removeItem('conversationId');
      this.createConversation();
    }
  }

  async handleSubmit(e) {
    e.preventDefault();

    const message = this.messageInput.value.trim();
    if (!message) return;

    // Add user message to history
    this.addMessage(message, 'user');

    // Clear input and disable form
    this.messageInput.value = '';
    this.setLoading(true);

    try {
      await this.sendMessage(message);
    } catch (error) {
      console.error('Error sending message:', error);
      this.addMessage('Sorry, there was an error processing your message. Please try again.', 'assistant');
    } finally {
      this.setLoading(false);
    }
  }

  async sendMessage(message) {
    if (!this.conversationId) {
      throw new Error('No conversation ID available');
    }

    // Add typing indicator
    const typingIndicator = this.addMessage('Assistant is thinking...', 'typing');

    try {
      const response = await fetch(`${__SERVICE_BASE__}${this.apiPrefix}/${this.conversationId}/chat`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          message: message
        })
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      // Remove typing indicator
      typingIndicator.remove();

      // Handle streaming response
      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let assistantMessage = this.addMessage('', 'assistant');
      let assistantText = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value);
        const lines = chunk.split('\n');

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const data = line.slice(6);

            try {
              const parsed = JSON.parse(data);
              if (parsed.deltaText) {
                assistantText += parsed.deltaText;
                // Render the accumulated markdown text
                assistantMessage.innerHTML = marked.parse(assistantText);
              }
            } catch (parseError) {
              // Ignore parsing errors for malformed chunks
            }
          }
        }
      }

    } catch (error) {
      // Remove typing indicator if it exists
      if (typingIndicator && typingIndicator.parentNode) {
        typingIndicator.remove();
      }
      throw error;
    }
  }

  addMessage(text, type) {
    // Remove welcome message if it exists
    const welcomeMessage = this.conversationHistory.querySelector('.welcome-message');
    if (welcomeMessage) {
      welcomeMessage.remove();
    }

    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${type}-message`;

    // Render markdown for assistant messages, plain text for user messages
    if (type === 'assistant' && text) {
      messageDiv.innerHTML = marked.parse(text);
    } else {
      messageDiv.textContent = text;
    }

    this.conversationHistory.appendChild(messageDiv);
    this.scrollToBottom();

    return messageDiv;
  }

  setLoading(loading) {
    this.submitButton.disabled = loading;
    this.messageInput.disabled = loading;

    if (loading) {
      this.submitButton.textContent = 'Sending...';
    } else {
      this.submitButton.textContent = 'Send';
      this.messageInput.focus();
    }
  }

  scrollToBottom() {
    this.conversationHistory.scrollTop = this.conversationHistory.scrollHeight;
  }
}

// Initialize the chat bot when the page loads
document.addEventListener('DOMContentLoaded', () => {
  new ChatBot();
});

console.log('Chat Bot initialized!');
