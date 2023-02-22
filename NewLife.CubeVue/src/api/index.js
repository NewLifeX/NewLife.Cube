import configFun from './config.js'
import dbFun from './db.js'
import entityFun from './entity.js'
import menuFun from './menu.js'
import userFun from './user.js'

export default function(state) {
  let apis = {}
  Object.assign(apis, configFun(state))
  Object.assign(apis, dbFun(state))
  Object.assign(apis, entityFun(state))
  Object.assign(apis, menuFun(state))
  Object.assign(apis, userFun(state))

  return apis
}
