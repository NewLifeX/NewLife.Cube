<template>
  <el-row type="flex" justify="center">
    <el-col class="login-col">
      <!-- Login -->
      <div>
        <!-- Logo-->
        <el-row>
          <el-col :span="24" class="login-logo">
            <i class="el-icon-cloudy"></i>
          </el-col>
        </el-row>
        <template v-if="loginConfig.allowLogin">
          <el-form :model="loginForm" size="medium" class="cube-login">
            <!-- 登录-->
            <span class="heading text-primary">{{ displayName }} 登录</span>
            <el-form-item label="">
              <el-input
                v-model="loginForm.username"
                placeholder="用户名 / 邮箱"
                prefix-icon="el-icon-user"
              ></el-input>
            </el-form-item>

            <el-form-item label="">
              <el-input
                v-model="loginForm.password"
                placeholder="密码"
                prefix-icon="el-icon-lock"
                show-password
              ></el-input>
            </el-form-item>
            <el-form-item label="">
              <el-checkbox
                class="text text-primary"
                v-model="loginForm.remember"
              >
                记住我
              </el-checkbox>

              <template v-if="loginConfig.allowRegister">
                <div
                  style="display: inline-block; margin-top: 5px; float: right;"
                >
                  <a
                    href="#Register"
                    data-toggle="tab"
                    style="margin-left: auto; margin-right: auto; position: static; font-size: 15px; margin-top: 5px;"
                  >
                    <span>我要注册</span>
                  </a>
                </div>
              </template>
            </el-form-item>
          </el-form>

          <button class="btn" @click="login">
            登录
          </button>
        </template>
      </div>
      <!-- Login3 -->
      <div v-if="loginConfig.providers.length > 0">
        <el-row>
          <el-col :span="24" class="text-center">
            <p class="login3">
              <span class="left"></span>
              第三方登录
              <span class="right"></span>
            </p>
            <el-row>
              <el-col :sm="24">
                <a
                  v-for="(mi, i) in loginConfig.providers"
                  :key="i"
                  :title="mi.nickName || mi.name"
                  @click="ssoClick(getUrl(mi))"
                >
                  <img
                    v-if="mi.logo"
                    :src="getLogoUrl(mi.logo)"
                    style="width: 64px;height: 64px;"
                  />
                  <template v-else>{{ mi.nickName || mi.name }}</template>
                </a>
              </el-col>
            </el-row>
          </el-col>
        </el-row>
      </div>
    </el-col>
  </el-row>
</template>

<script>
export default {
  computed: {
    sysConfig() {
      return this.$store.getters.sysConfig
    },
    loginConfig() {
      let vm = this
      let loginConfig = vm.$store.getters.loginConfig
      if (!loginConfig) {
        loginConfig = {
          displayName: '魔方',
          logo: '', // 系统logo
          allowLogin: true,
          allowRegister: true,
          providers: []
        }
      }
      return loginConfig
    },
    urls() {
      return this.$store.getters.urls
    },
    apis() {
      return this.$store.getters.apis
    },
    redirect() {
      return this.$route.query.redirect
    },
    displayName() {
      return (
        (this.sysConfig && this.sysConfig.displayName) ||
        (this.loginConfig && this.loginConfig.displayName)
      )
    }
  },
  data() {
    return {
      loginForm: {
        username: null,
        password: null,
        remember: true
      }
    }
  },
  created() {
    let vm = this
    try {
      // 关闭所有弹窗
      vm.$messageBox.close()
    } catch (error) {}
    // 为了本地快速检查是否需要自动跳转第三方登录，使用缓存的配置立即进行检查跳转
    vm.autoAuthRedirect()

    // 获取一次登录设置，如果跳转了第三方登录，会被强制取消
    vm.apis.getLoginConfig().then((res) => {
      let cfg = res.data.data
      vm.$store.dispatch('setLoginConfig', cfg)
    })
  },
  methods: {
    login() {
      let vm = this
      vm.apis.login(vm.loginForm).then((response) => {
        const data = response.data.data
        let token = data.token
        vm.$store.dispatch('setToken', token)
        vm.$router.push({ path: vm.redirect || '/' }, () => {})
      })
    },
    ssoClick(url) {
      location.href = this.urls.baseUrl + url
      // location.href = urls.ssoUrl + url
    },
    getUrl(mi) {
      // console.log(mi)
      let vm = this
      // let name = 'NewLife.Cube'
      let url = `/Sso/Login?name=${mi.name}&source=front-end`
      // let url = `/sso/authorize?response_type=token&client_id=${name}`
      let redirect_uri = encodeURIComponent(
        location.origin +
          '/auth-redirect' +
          (vm.redirect ? '?redirect=' + vm.redirect : '')
      )
      url += `&redirect_uri=${redirect_uri}`
      return url
    },
    getLogoUrl(logo) {
      let vm = this
      if (logo.indexOf('http') !== 0) {
        logo = vm.urls.baseUrl + logo
      }
      return logo
    },
    autoAuthRedirect() {
      // 根据设置，如果不允许密码登录，且只有一个第三方登录，自动跳转
      let vm = this
      let loginConfig = vm.loginConfig
      if (
        loginConfig &&
        !loginConfig.allowLogin &&
        loginConfig.providers.length === 1
      ) {
        vm.ssoClick(vm.getUrl(loginConfig.providers[0]))
      }
    }
  }
}
</script>

<!-- Login -->
<style scoped>
.login-logo {
  text-align: center;
  font-size: 130px;
  color: #4ca6ff;
  margin-top: 50px;
}

.cube-login {
  background: #fff;
  padding-bottom: 0;
  border-radius: 15px;
  text-align: center;
}

.cube-login .heading {
  display: block;
  font-size: 24px;
  font-weight: 700;
  padding: 5px 0;
  margin-bottom: 20px;
  text-align: center;
}

.cube-login input {
  border-radius: 20px;
  box-shadow: none;
  padding: 0 20px 0 45px;
  height: 40px;
  transition: all 0.3s ease 0s;
}

.cube-login .text {
  float: left;
  margin-left: 7px;
  line-height: 20px;
  padding-top: 5px;
  text-transform: capitalize;
}

.cube-login a {
  position: absolute;
  top: 12px;
  right: 0px;
  font-size: 17px;
  color: #c8c8c8;
  transition: all 0.5s ease 0s;
  color: #4ca6ff;
}

.el-input input {
  border-radius: 20px;
  box-shadow: none;
  padding: 0 20px 0 45px;
  height: 40px;
  transition: all 0.3s ease 0s;
  font-size: 14px;
  line-height: 1.42857143;
  color: #555;
  background-color: #fff;
  background-image: none;
  border: 1px solid #ccc;
}

.btn {
  float: right;
  font-size: 14px;
  color: #fff;
  background: #00b4ef;
  /* border-radius: 30px; */
  border-radius: 4px;
  padding: 8px 50px;
  border: none;
  text-transform: capitalize;
  transition: all 0.5s ease 0s;
  margin: -25px 0 15px 0;
  width: 100%;
}

.text-primary {
  color: #337ab7;
}

label {
  display: inline-block;
  max-width: 100%;
  margin-bottom: 5px;
  font-weight: 700;
}
</style>
<!-- Login3 -->
<style scoped>
.text-center {
  text-align: center;
}

p.login3 {
  font-size: 22px;
  position: relative;
  width: 100%;
  color: #333;
}

p.login3 span {
  height: 1px;
  position: absolute;
  background-color: #928f8f;
  width: 35%;
  top: 50%;
}

p.login3 span.right {
  right: 65%;
}

p.login3 span.left {
  left: 65%;
}

@media screen and (max-width: 680px) {
  .login-col {
    width: 80%;
  }
}

@media screen and (min-width: 680px) and (max-width: 1680px) {
  .login-col {
    width: 30%;
  }
}

@media screen and (min-width: 1680px) {
  .login-col {
    width: 445px;
  }
}
</style>
