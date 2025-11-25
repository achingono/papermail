FROM node:22-alpine AS base
WORKDIR /app

# Install dependencies
FROM base AS dependencies
COPY src/PaperMail.Oidc/package*.json ./
RUN npm install --omit=dev

# Build stage
FROM base AS build
COPY src/PaperMail.Oidc/package*.json ./
RUN npm install
COPY src/PaperMail.Oidc/ ./

# Production stage
FROM base AS production
ENV NODE_ENV=production
COPY --from=dependencies /app/node_modules ./node_modules
COPY --from=build /app/index.js ./
COPY --from=build /app/package.json ./

EXPOSE 8080

CMD ["node", "index.js"]
