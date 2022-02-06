.code
MyProc1 proc
	add RCX, RDX
	mov RAX, RCX
	ret
MyProc1 endp

MyProc5 proc, x_param: REAL4, y_param: REAL4
	push    rbp
    mov     rbp, rsp
    movss   DWORD PTR [rbp-4], xmm0
    movss   DWORD PTR [rbp-8], xmm1
    movss   xmm0, DWORD PTR [rbp-4]
    mulss   xmm0, DWORD PTR [rbp-8] 
    pop     rbp
    ret
MyProc5 endp
end