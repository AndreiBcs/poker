<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
</script>

<template>
  <div class="home">

    <nav class="nav">
      <div class="nav-logo">♠ PokerApp</div>
      <div class="nav-actions">
        <template v-if="auth.isAuthenticated">
          <span class="nav-user">{{ auth.username }}</span>
          <button class="btn btn-primary" @click="router.push({ name: 'lobby' })">Go to Lobby</button>
        </template>
        <template v-else>
          <button class="btn btn-ghost" @click="router.push({ name: 'login' })">Login</button>
          <button class="btn btn-primary" @click="router.push({ name: 'register' })">Sign Up</button>
        </template>
      </div>
    </nav>

    <section class="hero">
      <div class="hero-bg">
        <div class="felt-pattern"></div>
        <div class="glow glow-left"></div>
        <div class="glow glow-right"></div>
      </div>

      <div class="hero-content">
        <div class="hero-badge">Free to play · No real money</div>
        <h1 class="hero-title">
          Play Poker.<br />
          <span class="hero-title-accent">No Stakes.</span><br />
          All Thrill.
        </h1>
        <p class="hero-sub">
          Multiplayer Texas Hold'em with up to 10 players.<br />
          Jump in, bluff freely, and enjoy the game.
        </p>
        <div class="hero-cta">
          <button
            class="btn btn-primary btn-lg"
            @click="router.push({ name: auth.isAuthenticated ? 'lobby' : 'register' })"
          >
            {{ auth.isAuthenticated ? 'Find a Table' : 'Play for Free' }}
          </button>
          <button class="btn btn-outline btn-lg" @click="router.push({ name: 'lobby' })">
            Browse Lobby
          </button>
        </div>
      </div>

      <div class="hero-cards">
        <div class="card-fan">
          <div class="playing-card c1"><span class="rank">A</span><span class="suit suit-spade">♠</span></div>
          <div class="playing-card c2"><span class="rank">K</span><span class="suit suit-heart">♥</span></div>
          <div class="playing-card c3"><span class="rank">Q</span><span class="suit suit-diamond">♦</span></div>
          <div class="playing-card c4"><span class="rank">J</span><span class="suit suit-club">♣</span></div>
          <div class="playing-card c5"><span class="rank">10</span><span class="suit suit-heart">♥</span></div>
        </div>
      </div>
    </section>

    <footer class="footer">
      <span>♠ PokerApp — Demo project, no real money involved</span>
    </footer>

  </div>
</template>

<style scoped>
.home {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

/* HERO */
.hero {
  position: relative;
  min-height: 520px;
  display: flex;
  align-items: center;
  padding: 4rem 2.5rem;
  overflow: hidden;
  gap: 2rem;
}

.hero-bg {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.felt-pattern {
  position: absolute;
  inset: 0;
  background-image: radial-gradient(circle at 1px 1px, rgba(255,255,255,0.025) 1px, transparent 0);
  background-size: 28px 28px;
}

.glow {
  position: absolute;
  width: 500px;
  height: 500px;
  border-radius: 50%;
  filter: blur(120px);
  opacity: 0.18;
}

.glow-left  { background: var(--color-gold);  top: -100px; left: -100px; }
.glow-right { background: var(--color-green); bottom: -100px; right: -50px; }

.hero-content {
  flex: 1;
  max-width: 560px;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  position: relative;
  z-index: 2;
  animation: fadeUp 0.7s ease both;
}

.hero-badge {
  display: inline-block;
  width: fit-content;
  background: rgba(201, 168, 76, 0.12);
  border: 1px solid rgba(201, 168, 76, 0.3);
  color: var(--color-gold);
  font-size: 0.8rem;
  font-weight: 500;
  padding: 5px 14px;
  border-radius: 99px;
  letter-spacing: 0.04em;
}

.hero-title {
  font-family: var(--font-display);
  font-size: clamp(2.4rem, 5vw, 3.8rem);
  font-weight: 900;
  line-height: 1.1;
  color: #f0e8d8;
}

.hero-title-accent { color: var(--color-gold); }

.hero-sub {
  font-size: 1.05rem;
  color: var(--color-text-muted);
  line-height: 1.7;
}

.hero-cta {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

/* CARD FAN */
.hero-cards {
  flex: 0 0 auto;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  z-index: 2;
  animation: fadeUp 0.9s ease both;
}

.card-fan {
  position: relative;
  width: 220px;
  height: 280px;
}

.playing-card {
  position: absolute;
  width: 90px;
  height: 130px;
  background: #fff;
  border-radius: var(--radius-md);
  border: 1px solid rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 8px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5);
  transition: transform 0.3s ease;
}

.playing-card:hover { transform: translateY(-8px) !important; }

.rank {
  font-family: var(--font-display);
  font-size: 1.2rem;
  font-weight: 700;
  color: #1a1a1a;
  line-height: 1;
}

.suit       { font-size: 1.5rem; align-self: flex-end; line-height: 1; }
.suit-heart, .suit-diamond { color: #c0392b; }
.suit-spade, .suit-club    { color: #1a1a1a; }

.c1 { bottom: 30px; left: 0px;   transform: rotate(-18deg); z-index: 1; }
.c2 { bottom: 50px; left: 30px;  transform: rotate(-8deg);  z-index: 2; }
.c3 { bottom: 60px; left: 65px;  transform: rotate(2deg);   z-index: 3; }
.c4 { bottom: 50px; left: 100px; transform: rotate(11deg);  z-index: 2; }
.c5 { bottom: 30px; left: 128px; transform: rotate(20deg);  z-index: 1; }

/* FOOTER */
.footer {
  margin-top: auto;
  padding: 1.25rem 2.5rem;
  border-top: 1px solid var(--color-border);
  font-size: 0.8rem;
  color: #444;
  text-align: center;
}

/* RESPONSIVE */
@media (max-width: 768px) {
  .hero         { flex-direction: column; padding: 3rem 1.5rem; text-align: center; }
  .hero-badge   { align-self: center; }
  .hero-cta     { justify-content: center; }
  .features     { grid-template-columns: 1fr; padding: 2rem 1.5rem; }
  .nav          { padding: 1rem 1.5rem; }
  .hero-cards   { display: none; }
}
</style>