PS5_HOST ?= ps5
PS5_PORT ?= 9021

ifdef PS5_PAYLOAD_SDK
    include $(PS5_PAYLOAD_SDK)/toolchain/prospero.mk
else
    $(error PS5_PAYLOAD_SDK is undefined)
endif

ELF := savemounter.elf

CFLAGS := -Wall -Werror
CFLAGS += -lSceUserService -lSceSystemService -lSceFsInternalForVsh

all: $(ELF)

$(ELF): main.c batch_ftp.c
	$(CC) $(CFLAGS) -o $@ $^

clean:
	rm -f $(ELF)

deploy: $(ELF)
	$(PS5_DEPLOY) -h $(PS5_HOST) -p $(PS5_PORT) $^
