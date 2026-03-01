# ChatUI - Web Frontend

This is the web frontend that provides the user interface for the chatbot experience. Built with vanilla JavaScript and Vite, it demonstrates how to create a responsive, real-time chat interface that works seamlessly with streaming APIs.

The UI includes an **implementation switcher** dropdown that lets users toggle between the Traditional (OpenAI SDK) and Agent Framework backends. Both use the same SSE streaming format, so switching only changes the API URL prefix (`/conversations/` vs `/af/conversations/`). Switching resets the conversation.

Note that the client is not the focus of this workshop.
