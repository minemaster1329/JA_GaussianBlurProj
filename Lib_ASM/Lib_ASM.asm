.code
CalculatePixelAsm proc
                        imul r9, r9                                             ;raising diameter variable to 2-nd power
                        xorps xmm0, xmm0                                        ;zeroing xmm register
                        mov r10, rcx                                            ;moving pointer to pixels array to r10
                        mov r11, rdx                                            ;moving pointer to weights array to r11
                        xor rcx, rcx                                            ;zeroing rcx
                        mov rdx, r9                                             ;moving diameter value to rdx
                        shr rdx, 3                                              ;dividing diameter by 4
loop_start3:            vxorps ymm1, ymm1, ymm1                                 ;zeroing xmm1
                        vxorps ymm3, ymm3, ymm3                                 ;zeroing xmm3
                        xorps xmm4, xmm4                                        ;zeroing xmm4

                        mov r8, rcx                                             ;moving rcx to r8
                        shl rcx, 5                                              ;shifting r8 by 4

                        insertps xmm3, dword ptr[r10+rcx], 0                    ;moving c-th element of pixels array to ymm3[0]
                        insertps xmm3, dword ptr[r10+rcx+ 4], 16                ;moving (c+1)-th element of pixels array to ymm3[1]
                        insertps xmm3, dword ptr[r10+rcx+ 8], 32                ;moving (c+2)-th element of pixels array to ymm3[2]
                        insertps xmm3, dword ptr[r10+rcx+ 12], 48               ;moving (c+3)-th element of pixels array to ymm3[3]
                        vinsertf128 ymm3,ymm3, xmm3, 1                          ;moving first 4 elements from xmm3 to upper part of ymm3
                        insertps xmm3, dword ptr[r10+rcx+ 16], 0                ;moving (c+4)-th element of pixels array to ymm3[3]
                        insertps xmm3, dword ptr[r10+rcx+ 20], 16               ;moving (c+5)-th element of pixels array to ymm3[3]
                        insertps xmm3, dword ptr[r10+rcx+ 24], 32               ;moving (c+6)-th element of pixels array to ymm3[3]
                        insertps xmm3, dword ptr[r10+rcx+ 28], 48               ;moving (c+7)-th element of pixels array to ymm3[3]

                        insertps xmm1, dword ptr[r11+rcx], 0                    ;moving c-th element of weights array to xmm1[0]
                        insertps xmm1, dword ptr[r11+rcx+ 4], 16                ;moving (c+1)-th element of weights array to xmm1[1]
                        insertps xmm1, dword ptr[r11+rcx+ 8], 32                ;moving (c+2)-th element of weights array to xmm1[2]
                        insertps xmm1, dword ptr[r11+rcx+ 12], 48               ;moving (c+3)-th element of weights array to xmm1[3]
                        vinsertf128 ymm1, ymm1,xmm1, 1                          ;moving first 4 elements from xmm1 to uppper part of ymm3
                        insertps xmm1, dword ptr[r11+rcx + 16], 0               ;moving (c+4)-th element of weights array to xmm1[4]
                        insertps xmm1, dword ptr[r11+rcx + 20], 16              ;moving (c+5)-th element of weights array to xmm1[5]
                        insertps xmm1, dword ptr[r11+rcx + 24], 32              ;moving (c+6)-th element of weights array to xmm1[6]
                        insertps xmm1, dword ptr[r11+rcx + 28], 48              ;moving (c+7)-th element of weights array to xmm1[7]

                        mov rcx, r8                                             ;restoring original value of rcx

                        vmulps ymm3, ymm3,ymm1                                  ;multiplying xmm3 vector by xmm1 and moving result to xmm3

                        EXTRACTPS r8d, xmm3, 0                                  ;extracting xmm3[0] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addss xmm0, xmm4                                        ;adding xmm3[0] to output variable 
                        
                        EXTRACTPS r8d, xmm3, 1                                 ;extracting xmm3[1] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addss xmm0, xmm4                                        ;adding xmm3[1] to output variable

                        EXTRACTPS r8d, xmm3, 2                                  ;extracting xmm3[2] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addss xmm0, xmm4                                        ;adding xmm3[2] to output variable

                        EXTRACTPS r8d, xmm3, 3                                  ;extracting xmm[3] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addss xmm0, xmm4                                        ;adding xmm3[3] to output variable

                        vextractf128 xmm3, ymm3, 1                              ;moving upper part of ymm3 register to xmm3 register

                        EXTRACTPS r8d, xmm3, 0                                  ;extracting xmm[4] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addss xmm0, xmm4                                        ;adding xmm3[4] to output variable

                        EXTRACTPS r8d, xmm3, 1                                  ;extracting xmm[5] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addss xmm0, xmm4                                        ;adding xmm3[5] to output variable

                        EXTRACTPS r8d, xmm3, 2                                  ;extracting xmm[6] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addss xmm0, xmm4                                        ;adding xmm3[6] to output variable

                        EXTRACTPS r8d, xmm3, 3                                  ;extracting xmm[7] and moving into r8d
                        movd xmm4, r8d                                          ;moving r8d value to xmm4
                        addss xmm0, xmm4                                        ;adding xmm3[7] to output variable

                        inc rcx                                                 ;increment rcx
                        cmp rcx, rdx                                            ;compare counter with diameter / 4
                        jl loop_start3                                          ;if counter is lower than diameter / 4, move to loop_start
                        shl rcx, 3                                              ;multiplying counter by 4
                        cmp rcx, r9                                             ;comparing counter with squared diameter
                        je end_p                                                ;if counter is equal to squared diameter, jomping to end of program
                        mov rdx, r9                                             ;moving diameter value to rdx
loop_start4:            xorps xmm1, xmm1                                        ;zeroing xmm1
                        movss xmm1, dword ptr[r10+rcx*4]                        ;moving n-th element of pixels array to xmm1
                        mulss xmm1, dword ptr[r11+rcx*4]                        ;multiplying xmm1 by n-th element of weights array
                        addps xmm0, xmm1                                        ;adding value to output register
                        inc rcx                                                 ;incrementing counter
                        cmp rcx, rdx                                            ;comparing counter with squared diameter
                        jl loop_start4                                          ;if counter is lower than squared diameter, jumping to begin of loop 2
                        divss xmm0, xmm2                                        ;divide output by sum of weights
end_p:                  ret                                                     ;return from procedure
CalculatePixelAsm endp
end