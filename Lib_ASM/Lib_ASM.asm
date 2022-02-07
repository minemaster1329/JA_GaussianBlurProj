.code
MyProc1 proc
	add RCX, RDX
	mov RAX, RCX
	ret
MyProc1 endp

MyProc5 proc
	push    rbp
    mov     rbp, rsp
    mulss   xmm0, xmm1 
    pop     rbp
    ret
MyProc5 endp
end