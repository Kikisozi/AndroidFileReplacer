/**
 * 安卓文件替换工具 - Web前端控制脚本
 * 提供项目管理、API密钥配置、文件上传等功能
 */

// 全局变量
const API_KEY_STORAGE_KEY = 'android_file_replacer_api_key';
let currentApiKey = localStorage.getItem(API_KEY_STORAGE_KEY) || '';
let projects = [];

// DOM元素
const apiKeyInput = document.getElementById('api-key');
const saveApiKeyBtn = document.getElementById('save-api-key');
const authStatusEl = document.getElementById('auth-status');
const projectsContainer = document.getElementById('projects-container');
const loadingEl = document.getElementById('loading-projects');
const noProjectsEl = document.getElementById('no-projects');
const projectListEl = document.getElementById('project-list');
const refreshBtn = document.getElementById('refresh-btn');
const uploadForm = document.getElementById('upload-form');
const projectNameInput = document.getElementById('project-name');
const targetPathInput = document.getElementById('target-path');
const fileInput = document.getElementById('file-input');
const fileNameDisplay = document.getElementById('file-name-display');
const uploadButton = document.getElementById('upload-button');

// 初始化
document.addEventListener('DOMContentLoaded', () => {
    // 设置已保存的API密钥
    if (currentApiKey) {
        apiKeyInput.value = currentApiKey;
        verifyApiKey(currentApiKey);
    }
    
    // 绑定事件
    saveApiKeyBtn.addEventListener('click', saveApiKey);
    refreshBtn.addEventListener('click', loadProjects);
    uploadForm.addEventListener('submit', handleFormSubmit);
    fileInput.addEventListener('change', handleFileSelect);
    
    // 加载项目列表
    loadProjects();
});

/**
 * 保存API密钥
 */
function saveApiKey() {
    const apiKey = apiKeyInput.value.trim();
    
    if (!apiKey) {
        showToast('请输入API密钥', 'danger');
        return;
    }
    
    // 验证API密钥
    verifyApiKey(apiKey, true)
        .then(isValid => {
            if (isValid) {
                // 保存到本地存储
                localStorage.setItem(API_KEY_STORAGE_KEY, apiKey);
                currentApiKey = apiKey;
                
                // 关闭模态框
                const modal = bootstrap.Modal.getInstance(document.getElementById('api-key-modal'));
                modal.hide();
                
                // 重新加载项目列表
                loadProjects();
            }
        });
}

/**
 * 验证API密钥
 * @param {string} apiKey - API密钥
 * @param {boolean} showMessage - 是否显示消息提示
 * @returns {Promise<boolean>} 是否有效
 */
async function verifyApiKey(apiKey, showMessage = false) {
    try {
        const response = await fetch('/api/projects', {
            headers: {
                'X-API-Key': apiKey
            }
        });
        
        const isValid = response.ok;
        
        // 更新认证状态UI
        if (isValid) {
            authStatusEl.className = 'navbar-text auth-status bg-success text-white';
            authStatusEl.innerHTML = '<i class="bi bi-shield-check"></i> 已验证';
            
            if (showMessage) {
                showToast('API密钥验证成功', 'success');
            }
        } else {
            authStatusEl.className = 'navbar-text auth-status bg-danger text-white';
            authStatusEl.innerHTML = '<i class="bi bi-shield-lock"></i> 未验证';
            
            if (showMessage) {
                showToast('API密钥无效', 'danger');
            }
        }
        
        return isValid;
    } catch (error) {
        console.error('API密钥验证出错:', error);
        
        authStatusEl.className = 'navbar-text auth-status bg-warning text-white';
        authStatusEl.innerHTML = '<i class="bi bi-shield-exclamation"></i> 连接错误';
        
        if (showMessage) {
            showToast('连接服务器失败', 'danger');
        }
        
        return false;
    }
}

/**
 * 加载项目列表
 */
async function loadProjects() {
    if (!currentApiKey) {
        showToast('请先配置API密钥', 'warning');
        return;
    }
    
    try {
        // 显示加载状态
        loadingEl.classList.remove('d-none');
        noProjectsEl.classList.add('d-none');
        projectListEl.innerHTML = '';
        
        const response = await fetch('/api/projects', {
            headers: {
                'X-API-Key': currentApiKey
            }
        });
        
        if (!response.ok) {
            throw new Error(`请求失败: ${response.status}`);
        }
        
        projects = await response.json();
        
        // 隐藏加载状态
        loadingEl.classList.add('d-none');
        
        // 检查是否有项目
        if (projects.length === 0) {
            noProjectsEl.classList.remove('d-none');
            return;
        }
        
        // 渲染项目列表
        renderProjects(projects);
    } catch (error) {
        console.error('加载项目列表出错:', error);
        loadingEl.classList.add('d-none');
        showToast('加载项目列表失败', 'danger');
    }
}

/**
 * 渲染项目列表
 * @param {Array} projects - 项目数组
 */
function renderProjects(projects) {
    projectListEl.innerHTML = '';
    
    projects.forEach(project => {
        const projectEl = document.createElement('div');
        projectEl.className = 'project-item p-3 mb-2 border rounded';
        projectEl.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <h5 class="mb-1">${escapeHtml(project.name)}</h5>
                    <p class="mb-1 text-secondary small">
                        <strong>目标路径:</strong> ${escapeHtml(project.targetPath)}
                    </p>
                    <p class="mb-0 small">
                        <span class="badge bg-secondary">${escapeHtml(project.fileName)}</span>
                        <span class="badge bg-light text-dark">${formatFileSize(project.fileSize)}</span>
                        <span class="badge bg-info">${formatDate(project.createdAt)}</span>
                    </p>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm btn-outline-primary download-btn" data-file-id="${project.fileId}" data-file-name="${project.fileName}">
                        <i class="bi bi-download"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger delete-btn" data-project-id="${project.id}" data-project-name="${project.name}">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </div>
        `;
        
        projectListEl.appendChild(projectEl);
        
        // 绑定下载按钮事件
        const downloadBtn = projectEl.querySelector('.download-btn');
        downloadBtn.addEventListener('click', () => downloadFile(project.fileId, project.fileName));
        
        // 绑定删除按钮事件
        const deleteBtn = projectEl.querySelector('.delete-btn');
        deleteBtn.addEventListener('click', () => confirmDeleteProject(project.id, project.name));
    });
}

/**
 * 处理文件选择
 * @param {Event} e - 事件对象
 */
function handleFileSelect(e) {
    const file = e.target.files[0];
    if (file) {
        fileNameDisplay.textContent = file.name;
    } else {
        fileNameDisplay.textContent = '选择文件...';
    }
}

/**
 * 处理表单提交
 * @param {Event} e - 事件对象
 */
async function handleFormSubmit(e) {
    e.preventDefault();
    
    if (!currentApiKey) {
        showToast('请先配置API密钥', 'warning');
        return;
    }
    
    const projectName = projectNameInput.value.trim();
    const targetPath = targetPathInput.value.trim();
    const file = fileInput.files[0];
    
    if (!projectName || !targetPath || !file) {
        showToast('请填写所有必填项并选择文件', 'warning');
        return;
    }
    
    try {
        // 禁用上传按钮
        uploadButton.disabled = true;
        uploadButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> 上传中...';
        
        // 创建表单数据
        const formData = new FormData();
        formData.append('name', projectName);
        formData.append('targetPath', targetPath);
        formData.append('file', file);
        
        // 发送请求
        const response = await fetch('/api/projects', {
            method: 'POST',
            headers: {
                'X-API-Key': currentApiKey
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`上传失败: ${response.status}`);
        }
        
        // 清空表单
        uploadForm.reset();
        fileNameDisplay.textContent = '选择文件...';
        
        showToast('项目创建成功', 'success');
        
        // 重新加载项目列表
        loadProjects();
    } catch (error) {
        console.error('创建项目出错:', error);
        showToast('创建项目失败', 'danger');
    } finally {
        // 恢复上传按钮
        uploadButton.disabled = false;
        uploadButton.innerHTML = '<i class="bi bi-upload me-2"></i>上传并创建项目';
    }
}

/**
 * 下载文件
 * @param {string} fileId - 文件ID
 * @param {string} fileName - 文件名
 */
async function downloadFile(fileId, fileName) {
    try {
        // 创建一个临时链接
        const link = document.createElement('a');
        link.href = `/api/files/${fileId}?apiKey=${encodeURIComponent(currentApiKey)}`;
        link.download = fileName;
        link.style.display = 'none';
        
        // 添加到文档并模拟点击
        document.body.appendChild(link);
        link.click();
        
        // 清理
        setTimeout(() => {
            document.body.removeChild(link);
        }, 100);
    } catch (error) {
        console.error('下载文件出错:', error);
        showToast('下载文件失败', 'danger');
    }
}

/**
 * 确认删除项目
 * @param {string} projectId - 项目ID
 * @param {string} projectName - 项目名称
 */
function confirmDeleteProject(projectId, projectName) {
    if (confirm(`确定要删除项目 "${projectName}" 吗？此操作不可撤销。`)) {
        deleteProject(projectId);
    }
}

/**
 * 删除项目
 * @param {string} projectId - 项目ID
 */
async function deleteProject(projectId) {
    try {
        const response = await fetch(`/api/projects/${projectId}`, {
            method: 'DELETE',
            headers: {
                'X-API-Key': currentApiKey
            }
        });
        
        if (!response.ok) {
            throw new Error(`删除失败: ${response.status}`);
        }
        
        showToast('项目删除成功', 'success');
        
        // 重新加载项目列表
        loadProjects();
    } catch (error) {
        console.error('删除项目出错:', error);
        showToast('删除项目失败', 'danger');
    }
}

/**
 * 显示提示消息
 * @param {string} message - 消息内容
 * @param {string} type - 消息类型（success, danger, warning等）
 */
function showToast(message, type = 'info') {
    // 检查是否已存在Toast容器
    let toastContainer = document.querySelector('.toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // 创建Toast元素
    const toastId = `toast-${Date.now()}`;
    const toast = document.createElement('div');
    toast.className = `toast align-items-center border-0 bg-${type}`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    toast.setAttribute('id', toastId);
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body text-white">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="关闭"></button>
        </div>
    `;
    
    toastContainer.appendChild(toast);
    
    // 显示Toast
    const toastInstance = new bootstrap.Toast(toast, { delay: 3000 });
    toastInstance.show();
    
    // 监听隐藏事件，移除元素
    toast.addEventListener('hidden.bs.toast', () => {
        toastContainer.removeChild(toast);
    });
}

/**
 * 格式化文件大小
 * @param {number} size - 文件大小（字节）
 * @returns {string} 格式化后的大小
 */
function formatFileSize(size) {
    if (size < 1024) {
        return size + ' B';
    } else if (size < 1024 * 1024) {
        return (size / 1024).toFixed(1) + ' KB';
    } else if (size < 1024 * 1024 * 1024) {
        return (size / (1024 * 1024)).toFixed(1) + ' MB';
    } else {
        return (size / (1024 * 1024 * 1024)).toFixed(1) + ' GB';
    }
}

/**
 * 格式化日期
 * @param {string} dateString - ISO日期字符串
 * @returns {string} 格式化后的日期
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    }).replace(/\//g, '-');
}

/**
 * 转义HTML特殊字符
 * @param {string} html - 原始字符串
 * @returns {string} 转义后的字符串
 */
function escapeHtml(html) {
    const div = document.createElement('div');
    div.textContent = html;
    return div.innerHTML;
} 