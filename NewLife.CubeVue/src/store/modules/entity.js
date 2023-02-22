const route = {
  state: {
    listFields: {},
    addFormFields: {},
    editFormFields: {},
    detailFields: {},
  },
  mutations: {
    SET_ListFields: (state, { key, fields }) => {
      state.listFields[key] = fields
    },
    SET_AddFormFields: (state, { key, fields }) => {
      state.addFormFields[key] = fields
    },
    SET_EditFormFields: (state, { key, fields }) => {
      state.editFormFields[key] = fields
    },
    SET_DetailFields: (state, { key, fields }) => {
      state.detailFields[key] = fields
    },
  },
  actions: {
    setListFields({ commit }, { key, fields }) {
      commit('SET_ListFields', { key, fields })
    },
    setAddFormFields({ commit }, { key, fields }) {
      commit('SET_AddFormFields', { key, fields })
    },
    setEditFormFields({ commit }, { key, fields }) {
      commit('SET_EditFormFields', { key, fields })
    },
    setDetailFields({ commit }, { key, fields }) {
      commit('SET_DetailFields', { key, fields })
    },
  },
}

export default route
