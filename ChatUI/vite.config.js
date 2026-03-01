export default {
  server: {
    port: parseInt(process.env.PORT) || 5173,
    host: true,
  },
  define: {
    __SERVICE_BASE__: JSON.stringify(process.env.services__chatbot__https__0 || process.env.services__chatbot__http__0),
  }
};