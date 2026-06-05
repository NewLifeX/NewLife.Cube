<template>
  <div class="page-not-found">
    <h1>404</h1>
    <h2>Page Not Found</h2>
    <p>The page you are looking for does not exist or has been moved.</p>
    <router-link to="/" class="home-link">Go to Home Page</router-link>
  </div>
</template>

<script lang="ts">
export default {
  name: 'PageNotFound',
}
</script>

<style scoped>
.page-not-found {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  text-align: center;
  padding: 0 20px;
}

h1 {
  font-size: 6rem;
  margin-bottom: 0;
  color: #e74c3c;
}

h2 {
  font-size: 2rem;
  margin-top: 0;
  margin-bottom: 20px;
}

p {
  margin-bottom: 30px;
  color: #666;
}

.home-link {
  padding: 10px 20px;
  background-color: #3498db;
  color: white;
  text-decoration: none;
  border-radius: 4px;
  transition: background-color 0.3s;
}

.home-link:hover {
  background-color: #2980b9;
}
</style>
